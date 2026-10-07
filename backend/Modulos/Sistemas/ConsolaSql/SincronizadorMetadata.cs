using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Sistemas.ConsolaSql
{
    /// <summary>
    /// Después de correr un script de consola, lee las tablas reales de sys_&lt;slug&gt; y actualiza la
    /// metadata (entidades, campos y relaciones) para que el diseñador refleje lo que existe en la base.
    /// </summary>
    public static class SincronizadorMetadata
    {
        public static MetadataSyncResult Sincronizar(SystemBaseContext context, int systemId, string schemaName)
        {
            var result = new MetadataSyncResult();
            var runtimeTables = LoadRuntimeTables(context, schemaName);
            if (runtimeTables.Count == 0)
                return result;

            var entities = context.Entities
                .Where(e => e.SystemId == systemId)
                .ToList();

            var entitiesByTable = entities
                .Where(e => !string.IsNullOrWhiteSpace(e.TableName))
                .ToDictionary(e => e.TableName!.ToLowerInvariant(), e => e);

            var nextSortOrder = entities.Count == 0 ? 1 : entities.Max(e => e.SortOrder) + 1;
            var utcNow = DateTime.UtcNow;

            foreach (var table in runtimeTables)
            {
                if (!entitiesByTable.TryGetValue(table.ToLowerInvariant(), out var entity))
                {
                    entity = new Entities
                    {
                        SystemId = systemId,
                        Name = ToPascalIdentifier(table),
                        DisplayName = ToDisplayName(table),
                        TableName = table,
                        SortOrder = nextSortOrder++,
                        IsActive = true,
                        CreatedAt = utcNow
                    };
                    context.Entities.Add(entity);
                    entities.Add(entity);
                    entitiesByTable[table.ToLowerInvariant()] = entity;
                    result.EntitiesCreated++;
                }
                else
                {
                    var changed = false;
                    if (string.IsNullOrWhiteSpace(entity.Name))
                    {
                        entity.Name = ToPascalIdentifier(table);
                        changed = true;
                    }
                    if (string.IsNullOrWhiteSpace(entity.DisplayName))
                    {
                        entity.DisplayName = ToDisplayName(table);
                        changed = true;
                    }
                    if (entity.SortOrder <= 0)
                    {
                        entity.SortOrder = nextSortOrder++;
                        changed = true;
                    }
                    if (changed)
                    {
                        entity.UpdatedAt = utcNow;
                        result.EntitiesUpdated++;
                    }
                }
            }

            context.SaveChanges();

            var syncedEntities = context.Entities
                .Where(e => e.SystemId == systemId && runtimeTables.Contains(e.TableName!))
                .ToList()
                .Where(e => !string.IsNullOrWhiteSpace(e.TableName))
                .ToDictionary(e => e.TableName!.ToLowerInvariant(), e => e);

            var syncedEntityIds = syncedEntities.Values.Select(e => e.Id).ToList();
            var existingFields = context.Fields
                .Where(f => syncedEntityIds.Contains(f.EntityId))
                .ToList();

            var fieldsByKey = existingFields.ToDictionary(
                f => BuildFieldKey(f.EntityId, f.ColumnName),
                f => f
            );

            var runtimeColumns = LoadRuntimeColumns(context, schemaName);
            foreach (var column in runtimeColumns)
            {
                if (!syncedEntities.TryGetValue(column.TableName.ToLowerInvariant(), out var entity))
                    continue;

                var key = BuildFieldKey(entity.Id, column.ColumnName);
                var mappedType = MapSqlTypeToFieldType(column.SqlType);
                var required = !column.IsNullable || column.IsPrimaryKey || column.IsIdentity;
                var maxLength = CalculateMaxLength(column.SqlType, column.MaxLengthBytes);
                var precision = mappedType == "decimal" ? (int?)column.PrecisionValue : null;
                var scale = mappedType == "decimal" ? (int?)column.ScaleValue : null;
                var defaultValue = NormalizeDefaultDefinition(column.DefaultDefinition);

                if (!fieldsByKey.TryGetValue(key, out var field))
                {
                    field = new Fields
                    {
                        EntityId = entity.Id,
                        Name = ToPascalIdentifier(column.ColumnName),
                        ColumnName = column.ColumnName,
                        DataType = mappedType,
                        Required = required,
                        MaxLength = maxLength,
                        Precision = precision,
                        Scale = scale,
                        DefaultValue = defaultValue,
                        IsPrimaryKey = column.IsPrimaryKey,
                        IsIdentity = column.IsIdentity,
                        IsUnique = column.IsUnique,
                        SortOrder = column.ColumnOrder,
                        CreatedAt = utcNow
                    };
                    context.Fields.Add(field);
                    fieldsByKey[key] = field;
                    result.FieldsCreated++;
                }
                else
                {
                    field.Name = string.IsNullOrWhiteSpace(field.Name) ? ToPascalIdentifier(column.ColumnName) : field.Name;
                    field.DataType = mappedType;
                    field.Required = required;
                    field.MaxLength = maxLength;
                    field.Precision = precision;
                    field.Scale = scale;
                    field.DefaultValue = defaultValue;
                    field.IsPrimaryKey = column.IsPrimaryKey;
                    field.IsIdentity = column.IsIdentity;
                    field.IsUnique = column.IsUnique;
                    field.SortOrder = column.ColumnOrder;
                    field.UpdatedAt = utcNow;
                    result.FieldsUpdated++;
                }
            }

            var existingRelations = context.Relations
                .Where(r => r.SystemId == systemId)
                .ToList();

            var relationsByKey = existingRelations.ToDictionary(
                r => BuildRelationKey(r.SourceEntityId, r.TargetEntityId, r.ForeignKey),
                r => r
            );

            var runtimeRelations = LoadRuntimeRelations(context, schemaName);
            foreach (var relation in runtimeRelations)
            {
                if (!syncedEntities.TryGetValue(relation.SourceTableName.ToLowerInvariant(), out var sourceEntity))
                    continue;
                if (!syncedEntities.TryGetValue(relation.TargetTableName.ToLowerInvariant(), out var targetEntity))
                    continue;

                var key = BuildRelationKey(sourceEntity.Id, targetEntity.Id, relation.ForeignKeyColumn);
                if (!relationsByKey.TryGetValue(key, out var existingRelation))
                {
                    var created = new Relations
                    {
                        SystemId = systemId,
                        SourceEntityId = sourceEntity.Id,
                        TargetEntityId = targetEntity.Id,
                        RelationType = "ManyToOne",
                        ForeignKey = relation.ForeignKeyColumn,
                        CascadeDelete = relation.CascadeDelete,
                        CreatedAt = utcNow
                    };
                    context.Relations.Add(created);
                    relationsByKey[key] = created;
                    result.RelationsCreated++;
                }
                else
                {
                    var changed = false;
                    if (!string.Equals(existingRelation.RelationType, "ManyToOne", StringComparison.OrdinalIgnoreCase))
                    {
                        existingRelation.RelationType = "ManyToOne";
                        changed = true;
                    }
                    if (!string.Equals(existingRelation.ForeignKey, relation.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase))
                    {
                        existingRelation.ForeignKey = relation.ForeignKeyColumn;
                        changed = true;
                    }
                    if (existingRelation.CascadeDelete != relation.CascadeDelete)
                    {
                        existingRelation.CascadeDelete = relation.CascadeDelete;
                        changed = true;
                    }

                    if (changed)
                        result.RelationsUpdated++;
                }
            }

            context.SaveChanges();
            return result;
        }

        private static List<string> LoadRuntimeTables(SystemBaseContext context, string schemaName)
        {
            const string sql = @"
SELECT t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @schema
ORDER BY t.name;";

            var tables = new List<string>();
            ExecuteReader(context, sql, cmd =>
            {
                AddParameter(cmd, "@schema", schemaName);
            }, reader =>
            {
                while (reader.Read())
                    tables.Add(reader.GetString(0));
            });
            return tables;
        }

        private static List<RuntimeColumnInfo> LoadRuntimeColumns(SystemBaseContext context, string schemaName)
        {
            const string sql = @"
SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS SqlType,
    c.max_length AS MaxLengthBytes,
    c.[precision] AS PrecisionValue,
    c.scale AS ScaleValue,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity,
    CASE WHEN EXISTS (
        SELECT 1
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.object_id = t.object_id
          AND i.is_primary_key = 1
          AND ic.column_id = c.column_id
    ) THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN EXISTS (
        SELECT 1
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.object_id = t.object_id
          AND i.is_unique = 1
          AND i.is_primary_key = 0
          AND ic.column_id = c.column_id
    ) THEN 1 ELSE 0 END AS IsUnique,
    dc.definition AS DefaultDefinition,
    c.column_id AS ColumnOrder
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
WHERE s.name = @schema
ORDER BY t.name, c.column_id;";

            var columns = new List<RuntimeColumnInfo>();
            ExecuteReader(context, sql, cmd =>
            {
                AddParameter(cmd, "@schema", schemaName);
            }, reader =>
            {
                while (reader.Read())
                {
                    columns.Add(new RuntimeColumnInfo
                    {
                        TableName = reader.GetString(0),
                        ColumnName = reader.GetString(1),
                        SqlType = reader.GetString(2),
                        MaxLengthBytes = reader.GetInt16(3),
                        PrecisionValue = reader.GetByte(4),
                        ScaleValue = reader.GetByte(5),
                        IsNullable = reader.GetBoolean(6),
                        IsIdentity = reader.GetBoolean(7),
                        IsPrimaryKey = reader.GetInt32(8) == 1,
                        IsUnique = reader.GetInt32(9) == 1,
                        DefaultDefinition = reader.IsDBNull(10) ? null : reader.GetString(10),
                        ColumnOrder = reader.GetInt32(11)
                    });
                }
            });
            return columns;
        }

        private static List<RuntimeRelationInfo> LoadRuntimeRelations(SystemBaseContext context, string schemaName)
        {
            const string sql = @"
SELECT
    src_t.name AS SourceTableName,
    src_c.name AS SourceColumnName,
    tgt_t.name AS TargetTableName,
    CAST(CASE WHEN fk.delete_referential_action = 1 THEN 1 ELSE 0 END AS BIT) AS CascadeDelete
FROM sys.foreign_keys fk
INNER JOIN sys.tables src_t ON src_t.object_id = fk.parent_object_id
INNER JOIN sys.schemas src_s ON src_s.schema_id = src_t.schema_id
INNER JOIN sys.tables tgt_t ON tgt_t.object_id = fk.referenced_object_id
INNER JOIN sys.schemas tgt_s ON tgt_s.schema_id = tgt_t.schema_id
INNER JOIN (
    SELECT
        constraint_object_id,
        MIN(parent_column_id) AS ParentColumnId,
        COUNT(*) AS ColumnCount
    FROM sys.foreign_key_columns
    GROUP BY constraint_object_id
) fkc ON fkc.constraint_object_id = fk.object_id AND fkc.ColumnCount = 1
INNER JOIN sys.columns src_c ON src_c.object_id = src_t.object_id AND src_c.column_id = fkc.ParentColumnId
WHERE src_s.name = @schema
  AND tgt_s.name = @schema
ORDER BY src_t.name, src_c.name;";

            var relations = new List<RuntimeRelationInfo>();
            ExecuteReader(context, sql, cmd =>
            {
                AddParameter(cmd, "@schema", schemaName);
            }, reader =>
            {
                while (reader.Read())
                {
                    relations.Add(new RuntimeRelationInfo
                    {
                        SourceTableName = reader.GetString(0),
                        ForeignKeyColumn = reader.GetString(1),
                        TargetTableName = reader.GetString(2),
                        CascadeDelete = reader.GetBoolean(3)
                    });
                }
            });
            return relations;
        }

        private static void ExecuteReader(
            SystemBaseContext context,
            string sql,
            Action<IDbCommand> bindParameters,
            Action<IDataReader> onRead)
        {
            var connection = context.Database.GetDbConnection();
            var openHere = connection.State != ConnectionState.Open;
            if (openHere)
                connection.Open();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                bindParameters(command);
                using var reader = command.ExecuteReader();
                onRead(reader);
            }
            finally
            {
                if (openHere)
                    connection.Close();
            }
        }

        private static void AddParameter(IDbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private static string BuildFieldKey(int entityId, string? columnName)
        {
            return $"{entityId}:{(columnName ?? string.Empty).Trim().ToLowerInvariant()}";
        }

        private static string BuildRelationKey(int sourceEntityId, int targetEntityId, string? foreignKey)
        {
            return $"{sourceEntityId}:{targetEntityId}:{(foreignKey ?? string.Empty).Trim().ToLowerInvariant()}";
        }

        private static string ToPascalIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Item";

            var parts = Regex.Split(value.Trim(), @"[^A-Za-z0-9]+")
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (parts.Count == 0)
                return "Item";

            var output = string.Concat(parts.Select(p =>
            {
                if (p.Length == 1)
                    return p.ToUpperInvariant();
                return char.ToUpperInvariant(p[0]) + p[1..];
            }));

            return char.IsDigit(output[0]) ? $"N{output}" : output;
        }

        private static string ToDisplayName(string value)
        {
            var pascal = ToPascalIdentifier(value);
            return Regex.Replace(pascal, "([a-z0-9])([A-Z])", "$1 $2");
        }

        private static string MapSqlTypeToFieldType(string? sqlType)
        {
            var normalized = (sqlType ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "int" or "bigint" or "smallint" or "tinyint" => "int",
                "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "decimal",
                "bit" => "bool",
                "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" or "time" => "datetime",
                "uniqueidentifier" => "guid",
                _ => "string"
            };
        }

        private static int? CalculateMaxLength(string sqlType, short maxLengthBytes)
        {
            if (maxLengthBytes < 0)
                return null;

            var normalized = (sqlType ?? string.Empty).Trim().ToLowerInvariant();
            var stringTypes = new HashSet<string>
            {
                "char", "varchar", "text", "nchar", "nvarchar", "ntext"
            };

            if (!stringTypes.Contains(normalized))
                return null;

            if (normalized.StartsWith("n"))
                return maxLengthBytes / 2;

            return maxLengthBytes;
        }

        private static string? NormalizeDefaultDefinition(string? definition)
        {
            if (string.IsNullOrWhiteSpace(definition))
                return null;

            var value = definition.Trim();
            while (value.StartsWith("(") && value.EndsWith(")") && value.Length >= 2)
            {
                value = value[1..^1].Trim();
            }

            return value;
        }

        private sealed class RuntimeColumnInfo
        {
            public string TableName { get; set; } = string.Empty;
            public string ColumnName { get; set; } = string.Empty;
            public string SqlType { get; set; } = string.Empty;
            public short MaxLengthBytes { get; set; }
            public byte PrecisionValue { get; set; }
            public byte ScaleValue { get; set; }
            public bool IsNullable { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsPrimaryKey { get; set; }
            public bool IsUnique { get; set; }
            public string? DefaultDefinition { get; set; }
            public int ColumnOrder { get; set; }
        }

        private sealed class RuntimeRelationInfo
        {
            public string SourceTableName { get; set; } = string.Empty;
            public string ForeignKeyColumn { get; set; } = string.Empty;
            public string TargetTableName { get; set; } = string.Empty;
            public bool CascadeDelete { get; set; }
        }

        public sealed class MetadataSyncResult
        {
            public int EntitiesCreated { get; set; }
            public int EntitiesUpdated { get; set; }
            public int FieldsCreated { get; set; }
            public int FieldsUpdated { get; set; }
            public int RelationsCreated { get; set; }
            public int RelationsUpdated { get; set; }
        }
    }
}
