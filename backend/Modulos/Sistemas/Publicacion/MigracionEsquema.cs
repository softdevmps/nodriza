using System.Data;
using System.Data.Common;
using System.Globalization;
using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Sistemas.Publicacion
{
    /// <summary>
    /// Calcula los cambios que hay que aplicar sobre tablas YA publicadas para que reflejen
    /// el diseño, sin perder ni truncar datos:
    /// - se aplican: agrandar textos/precisión, conversiones sin pérdida (int → decimal/texto, etc.);
    /// - se aplican si los datos lo permiten: achicar textos, pasar a obligatorio, índice único;
    /// - todo lo demás se informa como error y no se ejecuta nada.
    /// Las columnas que ya no están en el diseño NO se borran (no se pierden datos):
    /// pasan a opcionales para que el runtime pueda seguir insertando.
    /// Los renombrados de tablas y columnas los resuelve IdentidadEsquema: acá se lee con el nombre
    /// que tienen HOY en la base y las sentencias usan el nombre final (corren después de renombrar).
    /// </summary>
    internal static class MigracionEsquema
    {
        internal sealed class Plan
        {
            /// <summary>Renombrados (sp_rename): se ejecutan antes que todo lo demás.</summary>
            public List<string> Renombres { get; } = new();
            /// <summary>Descripción legible de los renombrados, para el log.</summary>
            public List<string> CambiosDeNombre { get; } = new();
            /// <summary>Tablas que todavía no existen (se crean vacías).</summary>
            public List<string> TablasNuevas { get; } = new();
            public List<string> Sentencias { get; } = new();
            public List<string> Errores { get; } = new();
        }

        private sealed record ColumnaReal(string Nombre, string Tipo, int Largo, int Precision, int Escala, bool Nullable, bool Identity, bool EsPk);

        private sealed record TipoDeseado(string Familia, string Sql, int Largo, int Precision, int Escala);

        public static Plan Calcular(DbConnection conn, string schema, IEnumerable<Entities> entidades)
        {
            var plan = new Plan();
            var lista = entidades.ToList();

            var ren = IdentidadEsquema.Calcular(conn, schema, lista);
            plan.Renombres.AddRange(ren.Sentencias);
            plan.CambiosDeNombre.AddRange(ren.Descripcion);
            plan.Errores.AddRange(ren.Errores);
            if (plan.Errores.Count > 0)
                return plan;

            foreach (var entidad in lista)
            {
                var tablaHoy = ren.Tabla(entidad);
                if (tablaHoy == null)
                {
                    plan.TablasNuevas.Add(entidad.TableName); // la crea SistemasPublicador
                    continue;
                }

                var lectura = $"[{schema}].[{tablaHoy}]";          // para consultar ahora
                var tabla = $"[{schema}].[{entidad.TableName}]";   // para las sentencias
                var reales = LeerColumnas(conn, schema, tablaHoy);
                var tieneFilas = Escalar(conn, $"SELECT TOP 1 1 FROM {lectura}") != null;
                var nombreEntidad = entidad.DisplayName ?? entidad.Name;

                foreach (var campo in entidad.Fields.OrderBy(f => f.SortOrder).ThenBy(f => f.Id))
                {
                    var deseado = Deseado(campo);
                    var obligatorio = campo.Required || campo.IsPrimaryKey || campo.IsIdentity;
                    var col = $"[{campo.ColumnName}]";
                    var columnaHoy = ren.Columna(campo);

                    if (columnaHoy == null || !reales.TryGetValue(columnaHoy, out var real))
                    {
                        if (campo.IsPrimaryKey || campo.IsIdentity)
                        {
                            plan.Errores.Add($"{nombreEntidad}: no se puede agregar la clave primaria/identity \"{campo.ColumnName}\" a una tabla existente.");
                            continue;
                        }

                        if (!obligatorio || !tieneFilas)
                        {
                            plan.Sentencias.Add($"ALTER TABLE {tabla} ADD {col} {deseado.Sql} {(obligatorio ? "NOT NULL" : "NULL")};");
                        }
                        else if (LiteralPorDefecto(campo, out var literal))
                        {
                            plan.Sentencias.Add($"ALTER TABLE {tabla} ADD {col} {deseado.Sql} NOT NULL CONSTRAINT [DF_{schema}_{entidad.TableName}_{campo.ColumnName}] DEFAULT ({literal});");
                        }
                        else
                        {
                            plan.Errores.Add($"{nombreEntidad}: el campo obligatorio \"{campo.ColumnName}\" es nuevo y la tabla ya tiene datos. Definí un valor por defecto válido o hacelo opcional.");
                        }
                        continue;
                    }

                    var cambiaTipo = !MismoTipo(real, deseado);
                    var cambiaNulidad = real.Nullable == obligatorio;

                    if ((real.EsPk || real.Identity) && (cambiaTipo || cambiaNulidad))
                    {
                        plan.Errores.Add($"{nombreEntidad}: no se puede modificar la clave primaria \"{campo.ColumnName}\" de una tabla publicada.");
                        continue;
                    }

                    if (cambiaTipo)
                    {
                        var motivo = PuedeConvertir(conn, lectura, columnaHoy, real, deseado);
                        if (motivo != null)
                        {
                            plan.Errores.Add($"{nombreEntidad}.{campo.ColumnName}: {motivo}");
                            continue;
                        }
                    }

                    if (obligatorio && real.Nullable &&
                        Escalar(conn, $"SELECT TOP 1 1 FROM {lectura} WHERE [{columnaHoy}] IS NULL") != null)
                    {
                        plan.Errores.Add($"{nombreEntidad}.{campo.ColumnName}: no puede pasar a obligatorio porque hay registros sin valor.");
                        continue;
                    }

                    if (cambiaTipo || cambiaNulidad)
                    {
                        plan.Sentencias.AddRange(SoltarDependencias(conn, schema, tablaHoy, columnaHoy, tabla, ren.ObjetosSoltados));
                        plan.Sentencias.Add($"ALTER TABLE {tabla} ALTER COLUMN {col} {deseado.Sql} {(obligatorio ? "NOT NULL" : "NULL")};");
                    }
                }

                // Unicidad: crear lo que falta (si no hay duplicados) y quitar lo que ya no se pide.
                // (si la tabla o la columna se renombra, su índice ya se quitó en los renombrados y se vuelve a crear)
                foreach (var campo in entidad.Fields.Where(f => !f.IsPrimaryKey))
                {
                    var columnaHoy = ren.Columna(campo);
                    if (columnaHoy == null)
                        continue; // columna nueva: su índice lo crea SistemasPublicador
                    var indice = NombreIndiceUnico(schema, tablaHoy, columnaHoy);
                    var existe = !ren.ObjetosSoltados.Contains(indice) &&
                                 Escalar(conn, "SELECT 1 FROM sys.indexes WHERE name = @p0 AND object_id = OBJECT_ID(@p1)", indice, lectura) != null;
                    if (campo.IsUnique && !existe && reales.ContainsKey(columnaHoy) &&
                        Escalar(conn, $"SELECT TOP 1 1 FROM {lectura} WHERE [{columnaHoy}] IS NOT NULL GROUP BY [{columnaHoy}] HAVING COUNT(*) > 1") != null)
                    {
                        plan.Errores.Add($"{nombreEntidad}.{campo.ColumnName}: no puede ser único porque hay valores repetidos.");
                    }
                    else if (!campo.IsUnique && existe)
                    {
                        plan.Sentencias.Add($"DROP INDEX [{indice}] ON {tabla};");
                    }
                }

                // Columnas que ya no están en el diseño: se conservan con sus datos, pero opcionales.
                var enDiseno = entidad.Fields.Select(ren.Columna).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var huerfana in reales.Values.Where(c => !enDiseno.Contains(c.Nombre) && !c.Nullable && !c.EsPk && !c.Identity))
                {
                    plan.Sentencias.AddRange(SoltarDependencias(conn, schema, tablaHoy, huerfana.Nombre, tabla, ren.ObjetosSoltados));
                    plan.Sentencias.Add($"ALTER TABLE {tabla} ALTER COLUMN [{huerfana.Nombre}] {DefinicionReal(huerfana)} NULL;");
                }
            }

            return plan;
        }

        public static string NombreIndiceUnico(string schema, string tabla, string columna) => $"UX_{schema}_{tabla}_{columna}";

        /// <summary>Tipo SQL que corresponde a un campo del diseño (misma regla que al crear la tabla).</summary>
        public static string TipoSql(Fields campo) => Deseado(campo).Sql;

        private static TipoDeseado Deseado(Fields campo)
        {
            switch (campo.DataType?.ToLowerInvariant())
            {
                case "int": return new("int", "INT", 0, 10, 0);
                case "decimal":
                    var p = campo.Precision ?? 18;
                    var s = campo.Scale ?? 2;
                    return new("decimal", $"DECIMAL({p},{s})", 0, p, s);
                case "bool": return new("bool", "BIT", 0, 0, 0);
                case "datetime": return new("datetime", "DATETIME2", 0, 27, 7);
                case "guid": return new("guid", "UNIQUEIDENTIFIER", 0, 0, 0);
                default:
                    var largo = campo.MaxLength is > 0 ? campo.MaxLength.Value : 255;
                    return new("string", $"NVARCHAR({largo})", largo, 0, 0);
            }
        }

        private static string Familia(string tipoReal) => tipoReal switch
        {
            "nvarchar" or "varchar" or "nchar" or "char" or "ntext" or "text" => "string",
            "int" or "smallint" or "tinyint" => "int",
            "bigint" => "bigint",
            "decimal" or "numeric" => "decimal",
            "bit" => "bool",
            "datetime2" or "datetime" or "smalldatetime" or "date" => "datetime",
            "uniqueidentifier" => "guid",
            _ => tipoReal
        };

        private static bool MismoTipo(ColumnaReal real, TipoDeseado deseado) => deseado.Familia switch
        {
            "string" => real.Tipo == "nvarchar" && real.Largo == deseado.Largo,
            "decimal" => real.Tipo == "decimal" && real.Precision == deseado.Precision && real.Escala == deseado.Escala,
            "int" => real.Tipo == "int",
            "bool" => real.Tipo == "bit",
            "datetime" => real.Tipo == "datetime2",
            "guid" => real.Tipo == "uniqueidentifier",
            _ => false
        };

        /// <summary>null si la conversión es segura con los datos actuales; si no, el motivo.</summary>
        private static string? PuedeConvertir(DbConnection conn, string tabla, string columna, ColumnaReal real, TipoDeseado deseado)
        {
            var origen = Familia(real.Tipo);
            var col = $"[{columna}]";

            if (deseado.Familia == "string")
            {
                // Largo necesario para representar el valor actual como texto.
                var necesario = origen switch
                {
                    "string" => Convert.ToInt32(Escalar(conn, $"SELECT ISNULL(MAX(LEN({col})), 0) FROM {tabla}") ?? 0),
                    "int" => 11,
                    "bigint" => 20,
                    "decimal" => real.Precision + 2,
                    "bool" => 1,
                    "guid" => 36,
                    "datetime" => 27,
                    _ => -1
                };
                if (necesario < 0)
                    return $"no se puede convertir automáticamente de {real.Tipo} a texto.";
                return necesario <= deseado.Largo
                    ? null
                    : $"no se puede dejar en {deseado.Largo} caracteres: hay datos que necesitan {necesario}.";
            }

            if (deseado.Familia == "decimal")
            {
                var enteros = deseado.Precision - deseado.Escala;
                if (origen == "int" || origen == "bool")
                    return enteros >= 10 ? null : $"DECIMAL({deseado.Precision},{deseado.Escala}) no alcanza para los enteros existentes.";
                if (origen == "decimal")
                {
                    var ok = enteros >= real.Precision - real.Escala && deseado.Escala >= real.Escala;
                    return ok ? null : $"reducir la precisión de DECIMAL({real.Precision},{real.Escala}) a DECIMAL({deseado.Precision},{deseado.Escala}) puede perder datos.";
                }
            }

            if (deseado.Familia == "int" && (origen == "int" || origen == "bool"))
                return null;

            if (deseado.Familia == "datetime" && origen == "datetime")
                return null;

            if (deseado.Familia == origen && deseado.Familia is "bool" or "guid")
                return null;

            return $"no se puede convertir automáticamente de {real.Tipo} a {deseado.Sql} sin riesgo de perder datos.";
        }

        /// <summary>
        /// ALTER COLUMN falla si la columna tiene índices, FKs o defaults: se quitan los que maneja la
        /// fábrica (UX_/FK_/DF_ del schema). Los índices únicos y las FKs se vuelven a crear al final de la publicación.
        /// Se buscan con el nombre de hoy (tablaHoy/columna) y las sentencias usan la tabla final;
        /// lo que ya se quitó al renombrar (yaSoltados) no se repite.
        /// </summary>
        private static IEnumerable<string> SoltarDependencias(DbConnection conn, string schema, string tablaHoy, string columna, string tablaFinal, ISet<string> yaSoltados)
        {
            const string sql = @"
SELECT 'I', i.name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @p0 AND t.name = @p1 AND c.name = @p2 AND i.is_primary_key = 0 AND i.name LIKE 'UX[_]%'
UNION ALL
SELECT 'C', fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.tables t ON t.object_id = fk.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE s.name = @p0 AND t.name = @p1 AND c.name = @p2
UNION ALL
SELECT 'C', dc.name
FROM sys.default_constraints dc
JOIN sys.tables t ON t.object_id = dc.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE s.name = @p0 AND t.name = @p1 AND c.name = @p2;";

            var sentencias = new List<string>();
            using var cmd = Comando(conn, sql, schema, tablaHoy, columna);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var nombre = reader.GetString(1);
                if (yaSoltados.Contains(nombre))
                    continue;
                var objeto = "[" + nombre.Replace("]", "]]") + "]";
                sentencias.Add(reader.GetString(0) == "I"
                    ? $"DROP INDEX {objeto} ON {tablaFinal};"
                    : $"ALTER TABLE {tablaFinal} DROP CONSTRAINT {objeto};");
            }
            return sentencias;
        }

        private static bool LiteralPorDefecto(Fields campo, out string literal)
        {
            literal = string.Empty;
            var valor = campo.DefaultValue?.Trim();
            if (string.IsNullOrEmpty(valor))
                return false;

            var inv = CultureInfo.InvariantCulture;
            switch (campo.DataType?.ToLowerInvariant())
            {
                case "int":
                    if (!int.TryParse(valor, NumberStyles.AllowLeadingSign, inv, out var i)) return false;
                    literal = i.ToString(inv);
                    return true;
                case "decimal":
                    if (!decimal.TryParse(valor, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, inv, out var d)) return false;
                    literal = d.ToString(inv);
                    return true;
                case "bool":
                    if (valor is "1" or "0") { literal = valor; return true; }
                    if (!bool.TryParse(valor, out var b)) return false;
                    literal = b ? "1" : "0";
                    return true;
                case "datetime":
                    if (!DateTime.TryParse(valor, inv, DateTimeStyles.None, out var f)) return false;
                    literal = $"'{f:yyyy-MM-ddTHH:mm:ss.fffffff}'";
                    return true;
                case "guid":
                    if (!Guid.TryParse(valor, out var g)) return false;
                    literal = $"'{g}'";
                    return true;
                default:
                    literal = "N'" + valor.Replace("'", "''") + "'";
                    return true;
            }
        }

        private static string DefinicionReal(ColumnaReal c) => c.Tipo switch
        {
            "nvarchar" or "nchar" or "varchar" or "char" => $"{c.Tipo.ToUpperInvariant()}({(c.Largo < 0 ? "MAX" : c.Largo.ToString(CultureInfo.InvariantCulture))})",
            "decimal" or "numeric" => $"{c.Tipo.ToUpperInvariant()}({c.Precision},{c.Escala})",
            "datetime2" => $"DATETIME2({c.Escala})",
            _ => c.Tipo.ToUpperInvariant()
        };

        private static Dictionary<string, ColumnaReal> LeerColumnas(DbConnection conn, string schema, string tabla)
        {
            const string sql = @"
SELECT c.name, ty.name, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity,
       CAST(CASE WHEN EXISTS (
            SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            WHERE i.object_id = c.object_id AND i.is_primary_key = 1 AND ic.column_id = c.column_id) THEN 1 ELSE 0 END AS BIT)
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = @p0 AND t.name = @p1";

            var columnas = new Dictionary<string, ColumnaReal>(StringComparer.OrdinalIgnoreCase);
            using var cmd = Comando(conn, sql, schema, tabla);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var tipo = reader.GetString(1);
                var bytes = reader.GetInt16(2);
                var largo = bytes < 0 ? -1 : (tipo is "nvarchar" or "nchar" ? bytes / 2 : bytes);
                columnas[reader.GetString(0)] = new ColumnaReal(
                    reader.GetString(0), tipo, largo, reader.GetByte(3), reader.GetByte(4),
                    reader.GetBoolean(5), reader.GetBoolean(6), reader.GetBoolean(7));
            }
            return columnas;
        }

        private static object? Escalar(DbConnection conn, string sql, params object[] parametros)
        {
            using var cmd = Comando(conn, sql, parametros);
            var r = cmd.ExecuteScalar();
            return r == DBNull.Value ? null : r;
        }

        private static DbCommand Comando(DbConnection conn, string sql, params object[] parametros)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            for (var i = 0; i < parametros.Length; i++)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = $"@p{i}";
                p.Value = parametros[i];
                cmd.Parameters.Add(p);
            }
            return cmd;
        }
    }
}
