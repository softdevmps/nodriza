using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class EntidadesGestor
    {
        public sealed class EliminarEntidadResult
        {
            public bool Ok { get; set; }
            public bool NotFound { get; set; }
            public string? Error { get; set; }
            public bool RuntimeTableDropped { get; set; }
            public string Message { get; set; } = "Entidad eliminada.";
        }

        public static List<EntidadResponse> ObtenerPorSistema(int systemId)
        {
            using var context = new SystemBaseContext();

            return context.Entities
                .Where(e => e.SystemId == systemId)
                .OrderBy(e => e.SortOrder)
                .ThenBy(e => e.Id)
                .Select(e => new EntidadResponse
                {
                    Id = e.Id,
                    SystemId = e.SystemId,
                    Name = e.Name,
                    TableName = e.TableName,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    IsActive = e.IsActive,
                    SortOrder = e.SortOrder
                })
                .ToList();
        }

        public static List<EntidadResponse> ObtenerParaRuntime(int systemId, int usuarioId)
        {
            using var context = new SystemBaseContext();

            var allowed = PermisosGestor.ObtenerEntidadesPermitidas(context, usuarioId, systemId, "view");

            return context.Entities
                .Where(e => e.SystemId == systemId && allowed.Contains(e.Id))
                .OrderBy(e => e.SortOrder)
                .ThenBy(e => e.Id)
                .Select(e => new EntidadResponse
                {
                    Id = e.Id,
                    SystemId = e.SystemId,
                    Name = e.Name,
                    TableName = e.TableName,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    IsActive = e.IsActive,
                    SortOrder = e.SortOrder
                })
                .ToList();
        }

        public static EntidadResponse? ObtenerPorId(int systemId, int id)
        {
            using var context = new SystemBaseContext();

            var entity = context.Entities.FirstOrDefault(e => e.Id == id && e.SystemId == systemId);
            if (entity == null)
                return null;

            return new EntidadResponse
            {
                Id = entity.Id,
                SystemId = entity.SystemId,
                Name = entity.Name,
                TableName = entity.TableName,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder
            };
        }

        public static EntidadResponse? ObtenerPorNombre(int systemId, string name)
        {
            using var context = new SystemBaseContext();

            var entity = context.Entities.FirstOrDefault(e => e.SystemId == systemId && e.Name == name);
            if (entity == null)
                return null;

            return new EntidadResponse
            {
                Id = entity.Id,
                SystemId = entity.SystemId,
                Name = entity.Name,
                TableName = entity.TableName,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder
            };
        }

        public static int? Crear(int systemId, EntidadCreateRequest request)
        {
            using var context = new SystemBaseContext();

            var sistemaExiste = context.Systems.Any(s => s.Id == systemId);
            if (!sistemaExiste)
                return null;

            var name = request.Name.Trim();
            var tableName = request.TableName.Trim();

            var duplicated = context.Entities.Any(e =>
                e.SystemId == systemId &&
                (e.Name == name || e.TableName == tableName));

            if (duplicated)
                return null;

            var entidad = new Entities
            {
                SystemId = systemId,
                Name = name,
                TableName = tableName,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? name : request.DisplayName.Trim(),
                Description = request.Description?.Trim(),
                SortOrder = request.SortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Entities.Add(entidad);
            context.SaveChanges();

            return entidad.Id;
        }

        public static bool Editar(int systemId, int id, EntidadUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var entidad = context.Entities.FirstOrDefault(e => e.Id == id && e.SystemId == systemId);
            if (entidad == null)
                return false;

            var name = request.Name.Trim();
            var tableName = request.TableName.Trim();

            var duplicated = context.Entities.Any(e =>
                e.SystemId == systemId &&
                e.Id != id &&
                (e.Name == name || e.TableName == tableName));

            if (duplicated)
                return false;

            entidad.Name = name;
            entidad.TableName = tableName;
            entidad.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? name : request.DisplayName.Trim();
            entidad.Description = request.Description?.Trim();
            entidad.SortOrder = request.SortOrder;
            entidad.IsActive = request.IsActive;
            entidad.UpdatedAt = DateTime.UtcNow;

            context.SaveChanges();
            return true;
        }

        public static EliminarEntidadResult Eliminar(int systemId, int id, bool dropTable)
        {
            using var context = new SystemBaseContext();
            using var tx = context.Database.BeginTransaction();
            try
            {
                var entidad = context.Entities
                    .FirstOrDefault(e => e.Id == id && e.SystemId == systemId);

                if (entidad == null)
                {
                    return new EliminarEntidadResult
                    {
                        Ok = false,
                        NotFound = true,
                        Error = "Entidad no encontrada."
                    };
                }

                if (dropTable)
                {
                    var sistema = context.Systems.FirstOrDefault(s => s.Id == systemId);
                    if (sistema == null)
                    {
                        return new EliminarEntidadResult
                        {
                            Ok = false,
                            NotFound = true,
                            Error = "Sistema no encontrado."
                        };
                    }

                    var schemaName = ToSafeSchemaName(sistema.Slug);
                    var tableName = entidad.TableName?.Trim();
                    if (string.IsNullOrWhiteSpace(schemaName) || string.IsNullOrWhiteSpace(tableName))
                    {
                        return new EliminarEntidadResult
                        {
                            Ok = false,
                            Error = "No se pudo determinar schema o tabla runtime de la entidad."
                        };
                    }

                    DropRuntimeTable(context, schemaName, tableName);
                }

                var fields = context.Fields.Where(f => f.EntityId == entidad.Id).ToList();
                if (fields.Count > 0)
                    context.Fields.RemoveRange(fields);

                var relations = context.Relations
                    .Where(r => r.SystemId == systemId && (r.SourceEntityId == entidad.Id || r.TargetEntityId == entidad.Id))
                    .ToList();
                if (relations.Count > 0)
                    context.Relations.RemoveRange(relations);

                var modules = context.EntityModules.Where(m => m.EntityId == entidad.Id).ToList();
                if (modules.Count > 0)
                    context.EntityModules.RemoveRange(modules);

                var permissionKeys = PermisosGestor.Actions
                    .Select(action => PermisosGestor.BuildKey(entidad.Id, action))
                    .ToList();

                var permissions = context.Permissions
                    .Include(p => p.Role)
                    .Where(p => p.SystemId == systemId && p.Key != null && permissionKeys.Contains(p.Key))
                    .ToList();

                foreach (var permission in permissions)
                    permission.Role.Clear();

                if (permissions.Count > 0)
                    context.Permissions.RemoveRange(permissions);

                context.Entities.Remove(entidad);
                context.SaveChanges();
                tx.Commit();

                return new EliminarEntidadResult
                {
                    Ok = true,
                    RuntimeTableDropped = dropTable,
                    Message = dropTable
                        ? "Entidad eliminada y tabla runtime borrada."
                        : "Entidad eliminada (sin borrar tabla runtime)."
                };
            }
            catch (SqlException ex)
            {
                tx.Rollback();
                return new EliminarEntidadResult
                {
                    Ok = false,
                    Error = $"Error SQL al eliminar entidad: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return new EliminarEntidadResult
                {
                    Ok = false,
                    Error = $"Error al eliminar entidad: {ex.Message}"
                };
            }
        }

        private static string? ToSafeSchemaName(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var normalized = slug.Trim().ToLowerInvariant();
            if (normalized.Length == 0)
                return null;

            var safe = new string(normalized
                .Where(ch => char.IsLetterOrDigit(ch) || ch == '_')
                .ToArray());

            if (string.IsNullOrWhiteSpace(safe))
                return null;

            return $"sys_{safe}";
        }

        private static void DropRuntimeTable(SystemBaseContext context, string schemaName, string tableName)
        {
            const string sql = @"
DECLARE @schema SYSNAME = @p_schema;
DECLARE @table SYSNAME = @p_table;
DECLARE @qualified NVARCHAR(300) = QUOTENAME(@schema) + N'.' + QUOTENAME(@table);
DECLARE @objId INT = OBJECT_ID(@qualified, 'U');

IF @objId IS NOT NULL
BEGIN
    DECLARE @dropFkSql NVARCHAR(MAX) = N'';
    SELECT @dropFkSql = @dropFkSql +
        N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) +
        N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
    FROM sys.foreign_keys fk
    INNER JOIN sys.tables t ON t.object_id = fk.parent_object_id
    WHERE fk.parent_object_id = @objId OR fk.referenced_object_id = @objId;

    IF LEN(@dropFkSql) > 0
        EXEC sp_executesql @dropFkSql;

    DECLARE @dropTableSql NVARCHAR(MAX) = N'DROP TABLE ' + @qualified + N';';
    EXEC sp_executesql @dropTableSql;
END";

            context.Database.ExecuteSqlRaw(
                sql,
                new SqlParameter("@p_schema", schemaName),
                new SqlParameter("@p_table", tableName)
            );
        }
    }
}
