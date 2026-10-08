using System.Data.Common;
using System.Globalization;
using System.Text;
using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Sistemas.Publicacion
{
    /// <summary>
    /// Renombrados en el diseño sin perder datos.
    ///
    /// Cada tabla publicada lleva una propiedad extendida de SQL Server con el id de su entidad
    /// (nodriza_entidad) y cada columna, el id de su campo (nodriza_campo). Si en el diseño se cambia
    /// el TableName o el ColumnName, al publicar se reconoce la tabla/columna existente por su id y se
    /// renombra con sp_rename (que conserva datos y propiedades) en vez de crear otra vacía.
    ///
    /// - Las tablas publicadas antes de esto no tienen id: se reconocen por nombre y se etiquetan
    ///   en esa publicación. Desde la siguiente ya se pueden renombrar.
    /// - Intercambios (A↔B) pasan por un nombre temporal.
    /// - Los índices únicos (UX_) y las FKs (FK_) de la fábrica llevan el nombre de la tabla/columna:
    ///   los afectados se quitan antes de renombrar y la publicación los vuelve a crear con el nombre nuevo.
    /// - Si el nombre nuevo ya lo usa otra tabla/columna (que no se va a renombrar), se rechaza.
    /// </summary>
    internal static class IdentidadEsquema
    {
        public const string PropEntidad = "nodriza_entidad";
        public const string PropCampo = "nodriza_campo";

        internal sealed class Renombres
        {
            /// <summary>Se ejecutan primero, antes de crear tablas nuevas y de aplicar el resto de los cambios.</summary>
            public List<string> Sentencias { get; } = new();
            /// <summary>Para el log y el preview: "Tabla Clientes → Personas", "Personas.Nombre → NombreCompleto".</summary>
            public List<string> Descripcion { get; } = new();
            public List<string> Errores { get; } = new();
            /// <summary>Índices y FKs que se quitan acá (no hay que volver a quitarlos después).</summary>
            public HashSet<string> ObjetosSoltados { get; } = new(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<int, string> _tablas = new();
            private readonly Dictionary<int, string> _columnas = new();

            /// <summary>Nombre que tiene HOY en la base la tabla de la entidad (null si todavía no existe).</summary>
            public string? Tabla(Entities entidad) => _tablas.TryGetValue(entidad.Id, out var t) ? t : null;

            /// <summary>
            /// Nombre que tiene HOY en la base la columna del campo; null si el campo es nuevo (aunque exista
            /// una columna con su nombre: puede ser de otro campo que se renombra).
            /// </summary>
            public string? Columna(Fields campo) => _columnas.TryGetValue(campo.Id, out var c) ? c : null;

            internal void Tablas(Dictionary<int, string> v) { foreach (var (k, n) in v) _tablas[k] = n; }
            internal void Columnas(Dictionary<int, string> v) { foreach (var (k, n) in v) _columnas[k] = n; }
        }

        private sealed record Existente(string Nombre, int? Id);

        public static Renombres Calcular(DbConnection conn, string schema, IReadOnlyCollection<Entities> entidades)
        {
            var r = new Renombres();
            var sch = Q(schema);

            // ---- Tablas ----
            var tablas = Leer(conn, @"
SELECT t.name, TRY_CONVERT(INT, CAST(ep.value AS NVARCHAR(20)))
FROM sys.tables t
LEFT JOIN sys.extended_properties ep ON ep.class = 1 AND ep.major_id = t.object_id AND ep.minor_id = 0 AND ep.name = @p1
WHERE t.schema_id = SCHEMA_ID(@p0)", schema, PropEntidad);
            if (tablas.Count == 0)
                return r; // primera publicación

            var tablaActual = Emparejar(entidades.Select(e => (e.Id, e.TableName ?? string.Empty)), tablas);
            r.Tablas(tablaActual);
            var renTablas = Planear(entidades.Select(e => (e.Id, e.TableName ?? string.Empty)), tablaActual, tablas,
                (actual, final) => $"la tabla \"{final}\" ya existe en la base y no es de esta entidad (hoy se llama \"{actual}\").", r.Errores);

            // ---- Columnas (de cada tabla que ya existe) ----
            var renColumnas = new Dictionary<int, List<(int Id, string Actual, string Final)>>();
            foreach (var entidad in entidades)
            {
                var tabla = r.Tabla(entidad);
                if (tabla == null)
                    continue;

                var columnas = Leer(conn, @"
SELECT c.name, TRY_CONVERT(INT, CAST(ep.value AS NVARCHAR(20)))
FROM sys.columns c
LEFT JOIN sys.extended_properties ep ON ep.class = 1 AND ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = @p2
WHERE c.object_id = OBJECT_ID(QUOTENAME(@p0) + '.' + QUOTENAME(@p1))", schema, tabla, PropCampo);

                var campos = entidad.Fields.Select(f => (f.Id, f.ColumnName ?? string.Empty)).ToList();
                var columnaActual = Emparejar(campos, columnas);
                r.Columnas(columnaActual);
                var nombre = entidad.DisplayName ?? entidad.Name;
                var planCol = Planear(campos, columnaActual, columnas,
                    (actual, final) => $"{nombre}: la columna \"{final}\" ya existe en la tabla y no es de este campo (el campo hoy está en \"{actual}\"). Si es de un campo borrado, renombrala o borrala desde la consola SQL.", r.Errores);
                if (planCol.Count > 0)
                    renColumnas[entidad.Id] = planCol;
            }

            if (r.Errores.Count > 0 || (renTablas.Count == 0 && renColumnas.Count == 0))
                return r;

            // ---- 1) Quitar índices únicos y FKs de la fábrica que nombran lo que cambia ----
            var tablasQueCambian = renTablas.Select(t => t.Actual).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var columnasQueCambian = new HashSet<(string Tabla, string Columna)>();
            foreach (var entidad in entidades)
                if (renColumnas.TryGetValue(entidad.Id, out var cols))
                    foreach (var c in cols)
                        columnasQueCambian.Add((r.Tabla(entidad)!.ToLowerInvariant(), c.Actual.ToLowerInvariant()));
            bool Cambia(string tabla, string columna) =>
                tablasQueCambian.Contains(tabla) || columnasQueCambian.Contains((tabla.ToLowerInvariant(), columna.ToLowerInvariant()));

            foreach (var fk in Filas(conn, @"
SELECT fk.name, pt.name, pc.name, rt.name, rc.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE pt.schema_id = SCHEMA_ID(@p0) AND fk.name LIKE 'FK[_]%'", schema))
            {
                if ((Cambia(fk[1], fk[2]) || Cambia(fk[3], fk[4])) && r.ObjetosSoltados.Add(fk[0]))
                    r.Sentencias.Add($"ALTER TABLE {sch}.{Q(fk[1])} DROP CONSTRAINT {Q(fk[0])};");
            }

            foreach (var ux in Filas(conn, @"
SELECT i.name, t.name, c.name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
JOIN sys.tables t ON t.object_id = i.object_id
WHERE t.schema_id = SCHEMA_ID(@p0) AND i.is_primary_key = 0 AND i.name LIKE 'UX[_]%'", schema))
            {
                if (Cambia(ux[1], ux[2]) && r.ObjetosSoltados.Add(ux[0]))
                    r.Sentencias.Add($"DROP INDEX {Q(ux[0])} ON {sch}.{Q(ux[1])};");
            }

            // ---- 2) Renombrar tablas y después columnas (ya con el nombre final de la tabla) ----
            foreach (var (actual, final) in EnOrden(renTablas.Select(t => (t.Id, t.Actual, t.Final)), "t"))
                r.Sentencias.Add($"EXEC sp_rename N'{L(sch + "." + Q(actual))}', N'{L(final)}', N'OBJECT';");
            foreach (var t in renTablas)
                r.Descripcion.Add($"Tabla {t.Actual} → {t.Final}");

            foreach (var entidad in entidades)
            {
                if (!renColumnas.TryGetValue(entidad.Id, out var cols))
                    continue;
                var tabla = sch + "." + Q(entidad.TableName ?? string.Empty);
                foreach (var (actual, final) in EnOrden(cols, "c"))
                    r.Sentencias.Add($"EXEC sp_rename N'{L(tabla + "." + Q(actual))}', N'{L(final)}', N'COLUMN';");
                foreach (var c in cols)
                    r.Descripcion.Add($"{entidad.TableName}.{c.Actual} → {c.Final}");
            }

            return r;
        }

        /// <summary>
        /// Deja cada tabla y columna del diseño etiquetada con su id (se corre al final de la publicación,
        /// cuando ya existen todas con su nombre final).
        /// </summary>
        public static string ScriptEtiquetas(string schema, IEnumerable<Entities> entidades)
        {
            var sb = new StringBuilder();
            foreach (var entidad in entidades)
            {
                var tabla = entidad.TableName ?? string.Empty; // validados antes de publicar
                var objeto = L(Q(schema) + "." + Q(tabla));
                Etiqueta(sb, PropEntidad, entidad.Id, $"OBJECT_ID(N'{objeto}')", "0",
                    $"@level0type = N'SCHEMA', @level0name = N'{L(schema)}', @level1type = N'TABLE', @level1name = N'{L(tabla)}'");

                foreach (var campo in entidad.Fields)
                {
                    var columna = campo.ColumnName ?? string.Empty;
                    Etiqueta(sb, PropCampo, campo.Id, $"OBJECT_ID(N'{objeto}')", $"COLUMNPROPERTY(OBJECT_ID(N'{objeto}'), N'{L(columna)}', 'ColumnId')",
                        $"@level0type = N'SCHEMA', @level0name = N'{L(schema)}', @level1type = N'TABLE', @level1name = N'{L(tabla)}', @level2type = N'COLUMN', @level2name = N'{L(columna)}'");
                }
            }
            return sb.ToString();
        }

        private static void Etiqueta(StringBuilder sb, string propiedad, int id, string majorId, string minorId, string niveles)
        {
            var valor = id.ToString(CultureInfo.InvariantCulture);
            sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = {majorId} AND minor_id = {minorId} AND name = N'{propiedad}')");
            sb.AppendLine($"    EXEC sys.sp_updateextendedproperty @name = N'{propiedad}', @value = N'{valor}', {niveles};");
            sb.AppendLine("ELSE");
            sb.AppendLine($"    EXEC sys.sp_addextendedproperty @name = N'{propiedad}', @value = N'{valor}', {niveles};");
        }

        /// <summary>
        /// Qué tabla/columna existente le corresponde a cada elemento del diseño: primero por id
        /// (etiqueta de un elemento que sigue en el diseño) y, si no tiene, por nombre entre las libres.
        /// </summary>
        private static Dictionary<int, string> Emparejar(IEnumerable<(int Id, string Nombre)> diseno, List<Existente> existentes)
        {
            var lista = diseno.ToList();
            var ids = lista.Select(d => d.Id).ToHashSet();
            var resultado = new Dictionary<int, string>();
            var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in existentes.Where(e => e.Id is int id && ids.Contains(id)))
                if (resultado.TryAdd(e.Id!.Value, e.Nombre))
                    usados.Add(e.Nombre);

            foreach (var d in lista.Where(d => !resultado.ContainsKey(d.Id)))
            {
                var libre = existentes.FirstOrDefault(e => !usados.Contains(e.Nombre) && e.Nombre.Equals(d.Nombre, StringComparison.OrdinalIgnoreCase));
                if (libre != null)
                {
                    resultado[d.Id] = libre.Nombre;
                    usados.Add(libre.Nombre);
                }
            }
            return resultado;
        }

        /// <summary>Renombres necesarios; agrega un error si el nombre final lo ocupa algo que no se libera.</summary>
        private static List<(int Id, string Actual, string Final)> Planear(
            IEnumerable<(int Id, string Nombre)> diseno, Dictionary<int, string> actual, List<Existente> existentes,
            Func<string, string, string> mensajeOcupado, List<string> errores)
        {
            var renombres = diseno
                .Where(d => actual.TryGetValue(d.Id, out var a) && !string.Equals(a, d.Nombre, StringComparison.Ordinal))
                .Select(d => (d.Id, Actual: actual[d.Id], Final: d.Nombre))
                .ToList();

            var seLiberan = renombres.Select(x => x.Actual).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var (_, a, final) in renombres)
            {
                var ocupado = existentes.Any(e => e.Nombre.Equals(final, StringComparison.OrdinalIgnoreCase) &&
                                                  !e.Nombre.Equals(a, StringComparison.OrdinalIgnoreCase) && // solo cambia mayúsculas
                                                  !seLiberan.Contains(e.Nombre));
                if (ocupado)
                    errores.Add(mensajeOcupado(a, final));
            }
            return renombres;
        }

        /// <summary>Si algún destino es el nombre actual de otro que también cambia (A↔B), pasa por nombres temporales.</summary>
        private static IEnumerable<(string Actual, string Final)> EnOrden(IEnumerable<(int Id, string Actual, string Final)> renombres, string prefijo)
        {
            var lista = renombres.ToList();
            var actuales = lista.Select(x => x.Actual).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var cruzados = lista.Any(x => !x.Final.Equals(x.Actual, StringComparison.OrdinalIgnoreCase) && actuales.Contains(x.Final));
            if (!cruzados)
                return lista.Select(x => (x.Actual, x.Final));

            var temporal = (int id) => $"__nodriza_{prefijo}{id}";
            return lista.Select(x => (x.Actual, temporal(x.Id)))
                .Concat(lista.Select(x => (temporal(x.Id), x.Final)))
                .ToList();
        }

        private static List<Existente> Leer(DbConnection conn, string sql, params object[] parametros) =>
            Filas(conn, sql, parametros).Select(f => new Existente(f[0], f[1].Length == 0 ? null : int.Parse(f[1], CultureInfo.InvariantCulture))).ToList();

        /// <summary>Filas como texto (NULL → "").</summary>
        private static List<string[]> Filas(DbConnection conn, string sql, params object[] parametros)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            for (var i = 0; i < parametros.Length; i++)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = $"@p{i}";
                p.Value = parametros[i];
                cmd.Parameters.Add(p);
            }
            var filas = new List<string[]>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var fila = new string[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                    fila[i] = reader.IsDBNull(i) ? string.Empty : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) ?? string.Empty;
                filas.Add(fila);
            }
            return filas;
        }

        /// <summary>Identificador entre corchetes.</summary>
        private static string Q(string nombre) => "[" + nombre.Replace("]", "]]") + "]";

        /// <summary>Texto dentro de un literal N'...'.</summary>
        private static string L(string texto) => texto.Replace("'", "''");
    }
}
