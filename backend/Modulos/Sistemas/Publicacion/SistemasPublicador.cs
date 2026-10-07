using System.Text;
using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Sistemas.Publicacion.Modelos;

namespace Backend.Modulos.Sistemas.Publicacion
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

            var erroresRelaciones = ValidarRelaciones(system);
            if (erroresRelaciones.Count > 0)
                return Rechazar(context, system, erroresRelaciones);

            // Cambios sobre tablas ya publicadas: se calculan antes de tocar nada.
            var conn = context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();
            var plan = MigracionEsquema.Calcular(conn, schemaName, system.Entities);
            if (plan.Errores.Count > 0)
                return Rechazar(context, system, plan.Errores);

            using var trx = context.Database.BeginTransaction();
            try
            {
                context.Database.ExecuteSqlRaw(BuildScriptTablas(schemaName, system.Entities));
                foreach (var sentencia in plan.Sentencias)
                    context.Database.ExecuteSqlRaw(sentencia);
                var indices = BuildScriptIndices(schemaName, system.Entities);
                if (!string.IsNullOrWhiteSpace(indices))
                    context.Database.ExecuteSqlRaw(indices);

                AplicarRelaciones(context, schemaName, system);
                CrearMenusSistema(context, system);
                CrearPermisosSistema(context, system);
                // AsignarPermisosAdmin consulta la base: los permisos nuevos tienen que estar guardados
                // (dentro de la misma transacción); si no, en la primera publicación el Admin quedaba sin permisos.
                context.SaveChanges();
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
                    Log = plan.Sentencias.Count > 0
                        ? $"Published to schema {schemaName}. Cambios aplicados:\n{string.Join("\n", plan.Sentencias)}"
                        : $"Published to schema {schemaName}"
                };

                context.SystemBuilds.Add(build);
                context.SaveChanges();

                trx.Commit();

                return new PublicarResult
                {
                    Ok = true,
                    Message = plan.Sentencias.Count > 0
                        ? $"Sistema publicado en schema {schemaName}. Se aplicaron {plan.Sentencias.Count} cambios sobre tablas existentes."
                        : $"Sistema publicado en schema {schemaName}."
                };
            }
            catch (PublicacionException ex)
            {
                trx.Rollback();
                context.ChangeTracker.Clear();
                RegistrarBuildFallido(context, system, ex.Message);
                return new PublicarResult { Ok = false, Message = "No se publicó: " + ex.Message };
            }
            catch (Exception ex)
            {
                trx.Rollback();
                context.ChangeTracker.Clear();

                // El detalle técnico queda en el build; al cliente no se le devuelve el mensaje de SQL Server.
                var build = RegistrarBuildFallido(context, system, ex.Message);
                return new PublicarResult
                {
                    Ok = false,
                    Message = $"No se pudo publicar por un error inesperado de base de datos. No se aplicó ningún cambio. Detalle registrado en el build #{build}."
                };
            }
        }

        private static PublicarResult Rechazar(SystemBaseContext context, Systems system, List<string> errores)
        {
            RegistrarBuildFallido(context, system, string.Join("\n", errores));
            return new PublicarResult
            {
                Ok = false,
                Message = "No se publicó: " + string.Join(" ", errores)
            };
        }

        private static int RegistrarBuildFallido(SystemBaseContext context, Systems system, string log)
        {
            var build = new SystemBuilds
            {
                SystemId = system.Id,
                Status = "failed",
                Version = system.Version,
                StartedAt = DateTime.UtcNow,
                FinishedAt = DateTime.UtcNow,
                Log = log
            };
            context.SystemBuilds.Add(build);
            context.SaveChanges();
            return build.Id;
        }

        /// <summary>Relaciones publicables: tipo soportado, FK existente y del mismo tipo que la PK destino.</summary>
        private static List<string> ValidarRelaciones(Systems system)
        {
            var errores = new List<string>();
            var entidades = system.Entities.ToDictionary(e => e.Id);
            foreach (var rel in system.Relations)
            {
                if (!entidades.TryGetValue(rel.SourceEntityId, out var origen) || !entidades.TryGetValue(rel.TargetEntityId, out var destino))
                    continue;

                var nombre = $"{origen.Name} → {destino.Name}";
                if (!RelacionesSoportadas.Contains(rel.RelationType))
                {
                    errores.Add($"La relación {nombre} es {rel.RelationType}, que todavía no está soportada (solo ManyToOne y OneToOne).");
                    continue;
                }

                var fk = origen.Fields.FirstOrDefault(f => string.Equals(f.ColumnName, rel.ForeignKey?.Trim(), StringComparison.OrdinalIgnoreCase));
                var pkDestino = destino.Fields.FirstOrDefault(f => f.IsPrimaryKey);
                if (fk == null)
                    errores.Add($"La relación {nombre} usa la FK \"{rel.ForeignKey}\", que no es un campo de {origen.Name}.");
                else if (pkDestino == null)
                    errores.Add($"La relación {nombre} apunta a {destino.Name}, que no tiene clave primaria.");
                else if (!string.Equals(fk.DataType, pkDestino.DataType, StringComparison.OrdinalIgnoreCase))
                    errores.Add($"La relación {nombre}: la FK {fk.ColumnName} ({fk.DataType}) no es del mismo tipo que la clave de {destino.Name} ({pkDestino.DataType}).");
            }
            return errores;
        }

        /// <summary>Error de publicación con un mensaje apto para mostrarle al usuario.</summary>
        private sealed class PublicacionException : Exception
        {
            public PublicacionException(string message) : base(message) { }
        }

        public static readonly HashSet<string> RelacionesSoportadas = new(StringComparer.OrdinalIgnoreCase) { "ManyToOne", "OneToOne" };

        /// <summary>Crea el schema y las tablas que todavía no existen. Las existentes las ajusta MigracionEsquema.</summary>
        private static string BuildScriptTablas(string schemaName, IEnumerable<Entities> entities)
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
            }

            return sb.ToString();
        }

        /// <summary>
        /// Índices únicos que falten. Si la columna acepta NULL, el índice es filtrado
        /// (varios registros sin valor no cuentan como duplicados).
        /// </summary>
        private static string BuildScriptIndices(string schemaName, IEnumerable<Entities> entities)
        {
            var sb = new StringBuilder();
            foreach (var entity in entities)
            {
                var qualified = $"[{schemaName}].[{entity.TableName}]";
                foreach (var field in entity.Fields.Where(f => f.IsUnique && !f.IsPrimaryKey))
                {
                    var uxName = MigracionEsquema.NombreIndiceUnico(schemaName, entity.TableName, field.ColumnName);
                    var nullable = !(field.Required || field.IsIdentity);
                    var filtro = nullable ? $" WHERE [{field.ColumnName}] IS NOT NULL" : string.Empty;
                    sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{uxName}' AND object_id = OBJECT_ID('{qualified}'))");
                    sb.AppendLine($"    CREATE UNIQUE INDEX [{uxName}] ON {qualified} ([{field.ColumnName}]){filtro};");
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

                // Datos existentes que no cumplirían la FK: mensaje claro en vez del error de SQL.
                var huerfanos = context.Database.SqlQueryRaw<int>($@"
SELECT COUNT(*) AS Value FROM [{schemaName}].[{sourceTable}] s
WHERE s.[{fkColumn}] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [{schemaName}].[{targetTable}] t WHERE t.[{targetPk.ColumnName}] = s.[{fkColumn}])
  AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{constraintName}' AND parent_object_id = OBJECT_ID('[{schemaName}].[{sourceTable}]'))").AsEnumerable().First();
                if (huerfanos > 0)
                    throw new PublicacionException($"No se puede crear la relación {source.Name} → {target.Name}: {huerfanos} registro(s) de {source.Name} tienen en {fkColumn} un valor que no existe en {target.Name}.");

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
                var route = RutaMenu(system.Slug, entity.Name);

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

        /// <summary>Ruta del menú runtime de una entidad (la misma al crearlo y al borrarlo).</summary>
        public static string RutaMenu(string slug, string nombreEntidad) => $"/s/{slug}/{ToKebab(nombreEntidad)}";

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

        private static string MapSqlType(Fields field) => MigracionEsquema.TipoSql(field);

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
