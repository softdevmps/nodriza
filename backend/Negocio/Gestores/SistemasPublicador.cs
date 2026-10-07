using System.Text;
using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class SistemasPublicador
    {
        public static PublicarResult Publicar(int systemId)
        {
            using var context = new SystemBaseContext();

            var system = context.Systems
                .Include(s => s.Entities)
                .ThenInclude(e => e.Fields)
                .Include(s => s.Relations)
                .FirstOrDefault(s => s.Id == systemId);

            if (system == null)
            {
                return new PublicarResult
                {
                    Ok = false,
                    Message = "Sistema no encontrado."
                };
            }

            if (system.Entities.Count == 0)
            {
                return new PublicarResult
                {
                    Ok = false,
                    Message = "El sistema no tiene entidades."
                };
            }

            var schemaName = ToSafeSchemaName(system.Slug);
            if (schemaName == null)
            {
                return new PublicarResult
                {
                    Ok = false,
                    Message = "Slug invalido para crear schema."
                };
            }

            foreach (var entity in system.Entities)
            {
                if (entity.Fields.Count == 0)
                {
                    return new PublicarResult
                    {
                        Ok = false,
                        Message = $"Entidad sin campos: {entity.Name}"
                    };
                }

                if (ToSafeSqlName(entity.TableName) == null)
                {
                    return new PublicarResult
                    {
                        Ok = false,
                        Message = $"TableName invalido: {entity.TableName}"
                    };
                }

                foreach (var field in entity.Fields)
                {
                    if (ToSafeSqlName(field.ColumnName) == null)
                    {
                        return new PublicarResult
                        {
                            Ok = false,
                            Message = $"ColumnName invalido: {field.ColumnName}"
                        };
                    }
                }
            }

            var script = BuildScript(schemaName, system.Entities);

            using var trx = context.Database.BeginTransaction();
            try
            {
                context.Database.ExecuteSqlRaw(script);

                AplicarRelaciones(context, schemaName, system);
                CrearMenusSistema(context, system);
                CrearPermisosSistema(context, system);
                AsignarPermisosAdmin(context, system.Id);

                system.Status = "published";
                system.PublishedAt = DateTime.UtcNow;
                system.UpdatedAt = DateTime.UtcNow;

                var build = new SystemBuilds
                {
                    SystemId = system.Id,
                    Status = "success",
                    Version = system.Version,
                    StartedAt = DateTime.UtcNow,
                    FinishedAt = DateTime.UtcNow,
                    Log = $"Published to schema {schemaName}"
                };

                context.SystemBuilds.Add(build);
                context.SaveChanges();

                trx.Commit();

                return new PublicarResult
                {
                    Ok = true,
                    Message = $"Sistema publicado en schema {schemaName}."
                };
            }
            catch (Exception ex)
            {
                trx.Rollback();

                var build = new SystemBuilds
                {
                    SystemId = system.Id,
                    Status = "failed",
                    Version = system.Version,
                    StartedAt = DateTime.UtcNow,
                    FinishedAt = DateTime.UtcNow,
                    Log = ex.Message
                };

                context.SystemBuilds.Add(build);
                context.SaveChanges();

                return new PublicarResult
                {
                    Ok = false,
                    Message = $"Error al publicar: {ex.Message}"
                };
            }
        }

        private static string BuildScript(string schemaName, IEnumerable<Entities> entities)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{schemaName}')");
            sb.AppendLine($"EXEC('CREATE SCHEMA [{schemaName}]');");
            foreach (var entity in entities)
            {
                var tableName = entity.TableName;
                var qualified = $"[{schemaName}].[{tableName}]";

                sb.AppendLine($"IF OBJECT_ID('{qualified}', 'U') IS NULL");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    CREATE TABLE {qualified} (");

                var columnLines = new List<string>();
                var pkColumns = new List<string>();

                foreach (var field in entity.Fields.OrderBy(f => f.SortOrder).ThenBy(f => f.Id))
                {
                    var column = $"        [{field.ColumnName}] {MapSqlType(field)}";
                    if (field.IsIdentity && field.DataType.Equals("int", StringComparison.OrdinalIgnoreCase))
                        column += " IDENTITY(1,1)";

                    var notNull = field.Required || field.IsPrimaryKey || field.IsIdentity;
                    column += notNull ? " NOT NULL" : " NULL";

                    columnLines.Add(column);

                    if (field.IsPrimaryKey)
                        pkColumns.Add($"[{field.ColumnName}]");
                }

                if (pkColumns.Count > 0)
                {
                    var pkName = $"PK_{schemaName}_{tableName}";
                    columnLines.Add($"        CONSTRAINT [{pkName}] PRIMARY KEY ({string.Join(", ", pkColumns)})");
                }

                sb.AppendLine(string.Join(",\n", columnLines));
                sb.AppendLine("    );");
                sb.AppendLine("END");
                sb.AppendLine("ELSE");
                sb.AppendLine("BEGIN");

                foreach (var field in entity.Fields.OrderBy(f => f.SortOrder).ThenBy(f => f.Id))
                {
                    var colName = field.ColumnName;
                    var colType = MapSqlType(field);
                    var nullable = (field.IsIdentity || field.IsPrimaryKey) ? "NOT NULL" : "NULL";
                    var identity = field.IsIdentity && field.DataType.Equals("int", StringComparison.OrdinalIgnoreCase)
                        ? " IDENTITY(1,1)"
                        : "";

                    sb.AppendLine($"    IF COL_LENGTH('{qualified}', '{colName}') IS NULL");
                    sb.AppendLine($"        ALTER TABLE {qualified} ADD [{colName}] {colType}{identity} {nullable};");
                }

                sb.AppendLine("END");

                foreach (var field in entity.Fields.Where(f => f.IsUnique))
                {
                    var uxName = $"UX_{schemaName}_{tableName}_{field.ColumnName}";
                    sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{uxName}' AND object_id = OBJECT_ID('{qualified}'))");
                    sb.AppendLine($"    CREATE UNIQUE INDEX [{uxName}] ON {qualified} ([{field.ColumnName}]);");
                }

            }

            return sb.ToString();
        }

        private static void AplicarRelaciones(SystemBaseContext context, string schemaName, Systems system)
        {
            if (system.Relations.Count == 0)
                return;

            var entityMap = system.Entities.ToDictionary(e => e.Id, e => e);

            foreach (var relation in system.Relations)
            {
                if (!entityMap.TryGetValue(relation.SourceEntityId, out var source))
                    continue;
                if (!entityMap.TryGetValue(relation.TargetEntityId, out var target))
                    continue;

                if (string.IsNullOrWhiteSpace(relation.ForeignKey))
                    continue;

                var fkColumn = relation.ForeignKey.Trim();
                if (ToSafeSqlName(fkColumn) == null)
                    continue;

                var sourceTable = source.TableName;
                var targetTable = target.TableName;

                var targetPk = target.Fields.FirstOrDefault(f => f.IsPrimaryKey);
                if (targetPk == null)
                    continue;

                var constraintName = $"FK_{schemaName}_{sourceTable}_{targetTable}_{fkColumn}";
                if (constraintName.Length > 120)
                    constraintName = constraintName.Substring(0, 120);

                var sql = $@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{constraintName}' AND parent_object_id = OBJECT_ID('[{schemaName}].[{sourceTable}]'))
BEGIN
    ALTER TABLE [{schemaName}].[{sourceTable}]
    ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{fkColumn}])
    REFERENCES [{schemaName}].[{targetTable}] ([{targetPk.ColumnName}])
    {(relation.CascadeDelete ? "ON DELETE CASCADE" : "")};
END";

                context.Database.ExecuteSqlRaw(sql);
            }
        }

        private static void CrearMenusSistema(SystemBaseContext context, Systems system)
        {
            foreach (var entity in system.Entities)
            {
                var title = entity.DisplayName ?? entity.Name;
                var route = $"/s/{system.Slug}/{ToKebab(entity.Name)}";

                var exists = context.SystemMenus.Any(m =>
                    m.SystemId == system.Id &&
                    m.Route == route);

                if (exists)
                    continue;

                var menu = new SystemMenus
                {
                    SystemId = system.Id,
                    Title = title,
                    Route = route,
                    SortOrder = entity.SortOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.SystemMenus.Add(menu);
            }
        }

        private static void CrearPermisosSistema(SystemBaseContext context, Systems system)
        {
            foreach (var entity in system.Entities)
            {
                var entityName = entity.DisplayName ?? entity.Name;
                foreach (var action in PermisosGestor.Actions)
                {
                    var key = PermisosGestor.BuildKey(entity.Id, action);
                    var exists = context.Permissions.Any(p => p.SystemId == system.Id && p.Key == key);
                    if (exists)
                        continue;

                    var permiso = new Permissions
                    {
                        SystemId = system.Id,
                        Key = key,
                        Description = $"{entityName} - {ActionLabel(action)}",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Permissions.Add(permiso);
                }
            }
        }

        private static void AsignarPermisosAdmin(SystemBaseContext context, int systemId)
        {
            var adminRole = context.Roles
                .Include(r => r.Permission)
                .FirstOrDefault(r => r.Nombre.ToLower() == "admin");

            if (adminRole == null)
                return;

            var systemPermissions = context.Permissions
                .Where(p => p.SystemId == systemId)
                .ToList();

            if (systemPermissions.Count == 0)
                return;

            var existing = adminRole.Permission.Select(p => p.Id).ToHashSet();
            foreach (var permission in systemPermissions)
            {
                if (!existing.Contains(permission.Id))
                {
                    adminRole.Permission.Add(permission);
                }
            }
        }

        private static string ActionLabel(string action)
        {
            return action switch
            {
                "view" => "Ver",
                "create" => "Crear",
                "edit" => "Editar",
                "delete" => "Eliminar",
                _ => action
            };
        }

        private static string ToKebab(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "item";

            var sb = new StringBuilder();
            var prevDash = false;

            foreach (var ch in value.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (char.IsUpper(ch) && sb.Length > 0 && !prevDash)
                        sb.Append('-');

                    sb.Append(char.ToLowerInvariant(ch));
                    prevDash = false;
                }
                else
                {
                    if (!prevDash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevDash = true;
                    }
                }
            }

            var result = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(result) ? "item" : result;
        }

        private static string MapSqlType(Fields field)
        {
            var type = field.DataType?.ToLowerInvariant();
            return type switch
            {
                "string" => $"NVARCHAR({(field.MaxLength.HasValue && field.MaxLength > 0 ? field.MaxLength.Value.ToString() : "255")})",
                "int" => "INT",
                "decimal" => $"DECIMAL({field.Precision ?? 18},{field.Scale ?? 2})",
                "bool" => "BIT",
                "datetime" => "DATETIME2",
                "guid" => "UNIQUEIDENTIFIER",
                _ => "NVARCHAR(255)"
            };
        }

        private static string? ToSafeSchemaName(string slug)
        {
            var safe = ToSafeSqlName($"sys_{slug}");
            return safe;
        }

        private static string? ToSafeSqlName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var trimmed = input.Trim();
            var sb = new StringBuilder();

            foreach (var ch in trimmed)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    sb.Append(ch);
                }
                else
                {
                    return null;
                }
            }

            if (sb.Length == 0)
                return null;

            if (char.IsDigit(sb[0]))
                return null;

            return sb.ToString();
        }
    }
}
