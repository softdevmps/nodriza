using Backend.Comun;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Sistemas.Publicacion;

namespace Backend.Modulos.Sistemas.Datos
{
    public static class DatosGestor
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "string",
            "int",
            "decimal",
            "bool",
            "datetime",
            "guid"
        };

        private static readonly HashSet<string> SoftDeleteNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "isactive",
            "activo",
            "active"
        };

        public static (bool Ok, string? Error, List<Dictionary<string, object?>> Data, bool SinPermiso) Listar(int systemId, int entityId, int? take, int? skip, int usuarioId)
        {
            using var context = new SystemBaseContext();

            var meta = LoadMetadata(context, systemId, entityId);
            if (!meta.Ok)
                return (false, meta.Error, new List<Dictionary<string, object?>>(), false);

            if (!PermisosGestor.UsuarioTienePermiso(context, usuarioId, systemId, entityId, "view"))
                return (false, "Sin permisos para ver.", new List<Dictionary<string, object?>>(), true);

            var schemaName = meta.SchemaName;
            var entity = meta.Entity;
            var fields = meta.Fields;
            var pk = meta.Pk;

            var columns = fields.Select(f => $"[{f.ColumnName}]");
            var softDeleteField = GetSoftDeleteField(fields);
            var sql = new StringBuilder();
            sql.Append($"SELECT {string.Join(", ", columns)} FROM [{schemaName}].[{entity.TableName}]");
            if (softDeleteField != null)
            {
                sql.Append($" WHERE [{softDeleteField.ColumnName}] = 1");
            }
            var desde = Math.Max(0, skip ?? 0);
            if ((take.HasValue && take.Value > 0) || desde > 0)
            {
                var pkColumn = pk?.ColumnName ?? fields.First().ColumnName;
                sql.Append($" ORDER BY [{pkColumn}] OFFSET {desde} ROWS");
                if (take.HasValue && take.Value > 0)
                    sql.Append($" FETCH NEXT {take.Value} ROWS ONLY");
            }

            var result = new List<Dictionary<string, object?>>();

            var conn = AbrirConexion(context);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql.ToString();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in fields)
                {
                    var value = reader[field.ColumnName];
                    row[field.ColumnName] = value == DBNull.Value ? null : value;
                }

                result.Add(row);
            }

            return (true, null, result, false);
        }

        public static (bool Ok, string? Error, bool SinPermiso) Crear(int systemId, int entityId, Dictionary<string, JsonElement> data, int usuarioId)
        {
            data = SinDistinguirMayusculas(data);

            using var context = new SystemBaseContext();

            var meta = LoadMetadata(context, systemId, entityId);
            if (!meta.Ok)
                return (false, meta.Error, false);

            if (!PermisosGestor.UsuarioTienePermiso(context, usuarioId, systemId, entityId, "create"))
                return (false, "Sin permisos para crear.", true);

            var schemaName = meta.SchemaName;
            var entity = meta.Entity;
            var fields = meta.Fields;
            var pk = meta.Pk;

            var columns = new List<string>();
            var parameters = new List<IDbDataParameter>();
            var values = new List<string>();
            var softDeleteField = GetSoftDeleteField(fields);

            foreach (var field in fields)
            {
                if (field.IsIdentity)
                    continue;

                var key = field.ColumnName;
                if (!data.TryGetValue(key, out var element))
                {
                    if (softDeleteField != null && field.Id == softDeleteField.Id)
                    {
                        var paramNameSoft = $"@p{parameters.Count}";
                        columns.Add($"[{field.ColumnName}]");
                        values.Add(paramNameSoft);
                        parameters.Add(CreateParameter(paramNameSoft, true));
                        continue;
                    }

                    if (field.Required || field.IsPrimaryKey)
                        return (false, $"Campo requerido: {field.ColumnName}", false);
                    continue;
                }

                var validation = ValidateAndConvert(field, element, field.Required || field.IsPrimaryKey);
                if (!validation.Ok)
                    return (false, validation.Error, false);

                var value = validation.Value ?? DBNull.Value;
                if (field.IsUnique && value != DBNull.Value)
                {
                    var exists = ExistsByValue(context, schemaName, entity.TableName, field.ColumnName, value, null, null);
                    if (exists)
                        return (false, $"Valor duplicado en {field.ColumnName}", false);
                }

                var paramName = $"@p{parameters.Count}";
                columns.Add($"[{field.ColumnName}]");
                values.Add(paramName);
                parameters.Add(CreateParameter(paramName, value));
            }

            if (columns.Count == 0)
                return (false, "No hay columnas para insertar.", false);

            var sql = $"INSERT INTO [{schemaName}].[{entity.TableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)});";

            var error = EjecutarTraduciendoErrores(context, sql, parameters, meta.SchemaName, entity, esBorrado: false);
            return error == null ? (true, null, false) : (false, error, false);
        }

        public static (bool Ok, string? Error, bool SinPermiso) Editar(int systemId, int entityId, string id, Dictionary<string, JsonElement> data, int usuarioId)
        {
            data = SinDistinguirMayusculas(data);

            using var context = new SystemBaseContext();

            var meta = LoadMetadata(context, systemId, entityId);
            if (!meta.Ok)
                return (false, meta.Error, false);

            if (!PermisosGestor.UsuarioTienePermiso(context, usuarioId, systemId, entityId, "edit"))
                return (false, "Sin permisos para editar.", true);

            var schemaName = meta.SchemaName;
            var entity = meta.Entity;
            var fields = meta.Fields;
            var pk = meta.Pk;
            if (pk == null)
                return (false, "Entidad sin PK.", false);

            var setParts = new List<string>();
            var parameters = new List<IDbDataParameter>();

            var pkValue = ConvertPrimaryKey(id, pk);
            if (pkValue == InvalidValue)
                return (false, "Id invalido.", false);

            foreach (var field in fields)
            {
                if (field.IsPrimaryKey || field.IsIdentity)
                    continue;

                var key = field.ColumnName;
                if (!data.TryGetValue(key, out var element))
                    continue;

                var validation = ValidateAndConvert(field, element, field.Required);
                if (!validation.Ok)
                    return (false, validation.Error, false);

                var value = validation.Value ?? DBNull.Value;
                if (field.IsUnique && value != DBNull.Value)
                {
                    var exists = ExistsByValue(context, schemaName, entity.TableName, field.ColumnName, value, pk, pkValue);
                    if (exists)
                        return (false, $"Valor duplicado en {field.ColumnName}", false);
                }

                var paramName = $"@p{parameters.Count}";
                setParts.Add($"[{field.ColumnName}] = {paramName}");
                parameters.Add(CreateParameter(paramName, value));
            }

            if (setParts.Count == 0)
                return (false, "No hay columnas para actualizar.", false);

            var pkParam = $"@p{parameters.Count}";
            parameters.Add(CreateParameter(pkParam, pkValue));

            var sql = $"UPDATE [{schemaName}].[{entity.TableName}] SET {string.Join(", ", setParts)} WHERE [{pk.ColumnName}] = {pkParam};";
            var error = EjecutarTraduciendoErrores(context, sql, parameters, schemaName, entity, esBorrado: false);
            return error == null ? (true, null, false) : (false, error, false);
        }

        public static (bool Ok, string? Error, bool SinPermiso) Eliminar(int systemId, int entityId, string id, int usuarioId)
        {
            using var context = new SystemBaseContext();

            var meta = LoadMetadata(context, systemId, entityId);
            if (!meta.Ok)
                return (false, meta.Error, false);

            if (!PermisosGestor.UsuarioTienePermiso(context, usuarioId, systemId, entityId, "delete"))
                return (false, "Sin permisos para eliminar.", true);

            var schemaName = meta.SchemaName;
            var entity = meta.Entity;
            var fields = meta.Fields;
            var pk = meta.Pk;
            if (pk == null)
                return (false, "Entidad sin PK.", false);

            var pkValue = ConvertPrimaryKey(id, pk);
            if (pkValue == InvalidValue)
                return (false, "Id invalido.", false);

            var softDeleteField = GetSoftDeleteField(fields);
            if (softDeleteField != null)
            {
                var sqlSoft = $"UPDATE [{schemaName}].[{entity.TableName}] SET [{softDeleteField.ColumnName}] = 0 WHERE [{pk.ColumnName}] = @p0;";
                ExecuteNonQuery(context, sqlSoft, new[] { CreateParameter("@p0", pkValue) });
                return (true, null, false);
            }

            var blocking = TieneDependencias(context, systemId, schemaName, entityId, pkValue);
            if (blocking != null)
                return (false, $"No se puede eliminar: hay registros relacionados en {blocking}", false);

            var pkParam = "@p0";
            var sql = $"DELETE FROM [{schemaName}].[{entity.TableName}] WHERE [{pk.ColumnName}] = {pkParam};";
            var error = EjecutarTraduciendoErrores(context, sql, new[] { CreateParameter(pkParam, pkValue) }, schemaName, entity, esBorrado: true);
            return error == null ? (true, null, false) : (false, error, false);
        }

        /// <summary>
        /// Abre (si hace falta) la conexión del contexto. No se debe hacer "using" sobre ella:
        /// la libera el contexto, y destruirla rompía la consulta siguiente del mismo request.
        /// </summary>
        private static DbConnection AbrirConexion(SystemBaseContext context)
        {
            var conn = context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            return conn;
        }

        /// <summary>
        /// Ejecuta un INSERT/UPDATE/DELETE y traduce los errores de datos conocidos a un mensaje
        /// para el usuario (sin nombres internos de SQL Server). Devuelve null si salió bien.
        /// </summary>
        private static string? EjecutarTraduciendoErrores(SystemBaseContext context, string sql, IEnumerable<IDbDataParameter> parameters, string schemaName, Entities entity, bool esBorrado)
        {
            try
            {
                ExecuteNonQuery(context, sql, parameters);
                return null;
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                return "Ya existe un registro con ese valor (clave o campo único).";
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                if (esBorrado)
                    return "No se puede eliminar: hay registros relacionados.";

                var relacion = context.Relations
                    .Where(r => r.SourceEntityId == entity.Id && r.ForeignKey != null)
                    .AsEnumerable()
                    .FirstOrDefault(r => ex.Message.Contains($"_{r.ForeignKey}\"", StringComparison.OrdinalIgnoreCase));
                return relacion != null
                    ? $"El valor de {relacion.ForeignKey} no corresponde a ningún registro existente."
                    : "Un valor hace referencia a un registro que no existe.";
            }
            catch (SqlException ex) when (ex.Number is 8152 or 2628)
            {
                return "Un texto supera el largo máximo permitido.";
            }
            catch (SqlException ex) when (ex.Number is 8115 or 220 or 232)
            {
                return "Un número está fuera del rango permitido.";
            }
        }

        private static void ExecuteNonQuery(SystemBaseContext context, string sql, IEnumerable<IDbDataParameter> parameters)
        {
            var conn = AbrirConexion(context);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            cmd.ExecuteNonQuery();
        }

        private static object? ExecuteScalar(SystemBaseContext context, string sql, IEnumerable<IDbDataParameter> parameters)
        {
            var conn = AbrirConexion(context);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            return cmd.ExecuteScalar();
        }

        private static (bool Ok, string? Error, string SchemaName, Entities Entity, List<Fields> Fields, Fields? Pk) LoadMetadata(SystemBaseContext context, int systemId, int entityId)
        {
            var system = context.Systems.FirstOrDefault(s => s.Id == systemId);
            if (system == null)
                return (false, "Sistema no encontrado.", "", null!, new List<Fields>(), null);

            var schemaName = NombresSql.EsquemaDeSistema(system.Slug);
            if (schemaName == null)
                return (false, "Slug invalido.", "", null!, new List<Fields>(), null);

            var entity = context.Entities
                .Include(e => e.Fields)
                .FirstOrDefault(e => e.Id == entityId && e.SystemId == systemId);

            if (entity == null)
                return (false, "Entidad no encontrada.", "", null!, new List<Fields>(), null);

            if (NombresSql.Normalizar(entity.TableName) == null)
                return (false, "TableName invalido.", "", null!, new List<Fields>(), null);

            var fields = entity.Fields
                .Where(f => AllowedTypes.Contains(f.DataType))
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Id)
                .ToList();

            if (fields.Count == 0)
                return (false, "Entidad sin campos.", "", null!, new List<Fields>(), null);

            foreach (var field in fields)
            {
                if (NombresSql.Normalizar(field.ColumnName) == null)
                    return (false, $"ColumnName invalido: {field.ColumnName}", "", null!, new List<Fields>(), null);
            }

            var pk = fields.FirstOrDefault(f => f.IsPrimaryKey);
            return (true, null, schemaName, entity, fields, pk);
        }

        private static readonly object InvalidValue = new();

        private static object ConvertPrimaryKey(string id, Fields pk)
        {
            if (pk.DataType.Equals("int", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(id, out var v) ? v : InvalidValue;
            }

            if (pk.DataType.Equals("guid", StringComparison.OrdinalIgnoreCase))
            {
                return Guid.TryParse(id, out var v) ? v : InvalidValue;
            }

            return id;
        }

        /// <summary>
        /// Las columnas se reconocen sin importar mayúsculas: "email" y "Email" son la misma columna
        /// (los clientes JSON suelen mandar camelCase). Antes una clave en otro formato se ignoraba en silencio.
        /// </summary>
        private static Dictionary<string, JsonElement> SinDistinguirMayusculas(Dictionary<string, JsonElement> data)
        {
            var resultado = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var (clave, valor) in data)
                resultado.TryAdd(clave, valor);
            return resultado;
        }

        private const NumberStyles EstiloEntero = NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
        private const NumberStyles EstiloDecimal = EstiloEntero | NumberStyles.AllowDecimalPoint;

        private static object ConvertValue(JsonElement element, Fields field)
        {
            var type = field.DataType.ToLowerInvariant();

            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return DBNull.Value;

            // Los formularios mandan "" para un número o fecha vacíos: es nulo, no un valor inválido.
            if (type != "string" && element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString()))
                return DBNull.Value;

            try
            {
                return type switch
                {
                    "string" => element.GetString() ?? string.Empty,
                    // Cultura invariante y sin separador de miles: con la cultura del servidor (es-*)
                    // "10.50" se leía como 1050. Un formato ambiguo ("10,50", "1.000") se rechaza.
                    "int" => element.ValueKind == JsonValueKind.Number
                        ? element.GetInt32()
                        : int.Parse(element.GetString() ?? "0", EstiloEntero, CultureInfo.InvariantCulture),
                    "decimal" => element.ValueKind == JsonValueKind.Number
                        ? element.GetDecimal()
                        : decimal.Parse(element.GetString() ?? "0", EstiloDecimal, CultureInfo.InvariantCulture),
                    "bool" => element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False
                        ? element.GetBoolean()
                        : bool.Parse(element.GetString() ?? "false"),
                    "datetime" => element.ValueKind == JsonValueKind.String ? ParsearFecha(element.GetString()!) : element.GetDateTime(),
                    "guid" => Guid.Parse(element.GetString() ?? string.Empty),
                    _ => InvalidValue
                };
            }
            catch
            {
                return InvalidValue;
            }
        }

        private static readonly System.Text.RegularExpressions.Regex ConZonaHoraria =
            new(@"(Z|[+-]\d{2}:?\d{2})$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Fecha con zona (Z u offset): se guarda en UTC, el mismo instante. Sin zona: se guarda tal cual
        /// (hora "de pared" que cargó el usuario). Nunca depende de la zona horaria ni la cultura del servidor.
        /// </summary>
        private static DateTime ParsearFecha(string texto)
        {
            texto = texto.Trim();
            if (ConZonaHoraria.IsMatch(texto))
                return DateTimeOffset.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None).UtcDateTime;

            return DateTime.SpecifyKind(DateTime.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None), DateTimeKind.Unspecified);
        }

        private static (bool Ok, object? Value, string? Error) ValidateAndConvert(Fields field, JsonElement element, bool required)
        {
            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
            {
                if (required)
                    return (false, null, $"Campo requerido: {field.ColumnName}");

                return (true, DBNull.Value, null);
            }

            var value = ConvertValue(element, field);
            if (value == InvalidValue)
                return (false, null, $"Valor invalido para {field.ColumnName}");

            if (value == DBNull.Value)
            {
                if (required)
                    return (false, null, $"Campo requerido: {field.ColumnName}");
                return (true, DBNull.Value, null);
            }

            if (field.DataType.Equals("string", StringComparison.OrdinalIgnoreCase) && value is string str)
            {
                if (field.MaxLength.HasValue && field.MaxLength.Value > 0 && str.Length > field.MaxLength.Value)
                    return (false, null, $"Maximo {field.MaxLength.Value} caracteres en {field.ColumnName}");
            }

            if (field.DataType.Equals("decimal", StringComparison.OrdinalIgnoreCase) && value is decimal dec)
            {
                if (!DecimalWithinPrecision(dec, field.Precision, field.Scale))
                    return (false, null, $"Valor fuera de precision/scale en {field.ColumnName}");
            }

            return (true, value, null);
        }

        private static bool DecimalWithinPrecision(decimal value, int? precision, int? scale)
        {
            var str = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
            var parts = str.Split('.');
            var intPart = parts[0].TrimStart('0');
            var fracPart = parts.Length > 1 ? parts[1] : "";

            var totalDigits = intPart.Length + fracPart.Length;
            if (totalDigits == 0)
                totalDigits = 1;

            if (scale.HasValue && fracPart.Length > scale.Value)
                return false;

            if (precision.HasValue && totalDigits > precision.Value)
                return false;

            return true;
        }

        private static bool ExistsByValue(SystemBaseContext context, string schemaName, string tableName, string columnName, object value, Fields? pkField, object? pkValue)
        {
            var sql = new StringBuilder();
            sql.Append($"SELECT TOP 1 1 FROM [{schemaName}].[{tableName}] WHERE [{columnName}] = @p0");
            var parameters = new List<IDbDataParameter> { CreateParameter("@p0", value) };

            if (pkField != null && pkValue != null)
            {
                sql.Append($" AND [{pkField.ColumnName}] <> @p1");
                parameters.Add(CreateParameter("@p1", pkValue));
            }

            return ExecuteScalar(context, sql.ToString(), parameters) != null;
        }

        private static Fields? GetSoftDeleteField(IEnumerable<Fields> fields)
        {
            return fields.FirstOrDefault(f =>
                f.DataType.Equals("bool", StringComparison.OrdinalIgnoreCase) &&
                SoftDeleteNames.Contains(f.ColumnName));
        }

        private static string? TieneDependencias(SystemBaseContext context, int systemId, string schemaName, int entityId, object pkValue)
        {
            var relations = context.Relations
                .Where(r => r.SystemId == systemId && r.TargetEntityId == entityId && !r.CascadeDelete)
                .ToList();

            if (relations.Count == 0)
                return null;

            var entityMap = context.Entities
                .Where(e => e.SystemId == systemId)
                .ToDictionary(e => e.Id, e => e.TableName);

            foreach (var relation in relations)
            {
                if (string.IsNullOrWhiteSpace(relation.ForeignKey))
                    continue;

                var fk = relation.ForeignKey.Trim();

                if (!entityMap.TryGetValue(relation.SourceEntityId, out var sourceTable))
                    continue;

                if (NombresSql.Normalizar(sourceTable) == null || NombresSql.Normalizar(fk) == null)
                    continue;

                var sql = $"SELECT TOP 1 1 FROM [{schemaName}].[{sourceTable}] WHERE [{fk}] = @p0";
                var exists = ExecuteScalar(context, sql, new[] { CreateParameter("@p0", pkValue) });
                if (exists != null)
                    return sourceTable;
            }

            return null;
        }

        private static IDbDataParameter CreateParameter(string name, object value)
        {
            var param = new Microsoft.Data.SqlClient.SqlParameter
            {
                ParameterName = name,
                Value = value ?? DBNull.Value
            };
            if (value is DateTime)
                param.SqlDbType = SqlDbType.DateTime2;

            return param;
        }

    }
}
