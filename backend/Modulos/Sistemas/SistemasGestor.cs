using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Sistemas.Modelos;

namespace Backend.Modulos.Sistemas
{
    public static class SistemasGestor
    {
        public static List<SistemaResponse> ObtenerTodos()
        {
            using var context = new SystemBaseContext();

            return context.Systems
                .OrderBy(s => s.Id)
                .Select(s => new SistemaResponse
                {
                    Id = s.Id,
                    Slug = s.Slug,
                    Name = s.Name,
                    Namespace = s.Namespace,
                    Status = s.Status,
                    IsActive = s.IsActive,
                    Version = s.Version,
                    Description = s.Description
                })
                .ToList();
        }

        public static SistemaDetalleResponse? ObtenerPorId(int id)
        {
            using var context = new SystemBaseContext();

            var sistema = context.Systems.FirstOrDefault(s => s.Id == id);
            if (sistema == null)
                return null;

            return new SistemaDetalleResponse
            {
                Id = sistema.Id,
                Slug = sistema.Slug,
                Name = sistema.Name,
                Namespace = sistema.Namespace,
                Status = sistema.Status,
                IsActive = sistema.IsActive,
                Version = sistema.Version,
                Description = sistema.Description,
                CreatedAt = sistema.CreatedAt,
                UpdatedAt = sistema.UpdatedAt,
                PublishedAt = sistema.PublishedAt
            };
        }

        public static SistemaDetalleResponse? ObtenerPorSlug(string slug)
        {
            using var context = new SystemBaseContext();

            var sistema = context.Systems.FirstOrDefault(s => s.Slug == slug);
            if (sistema == null)
                return null;

            return new SistemaDetalleResponse
            {
                Id = sistema.Id,
                Slug = sistema.Slug,
                Name = sistema.Name,
                Namespace = sistema.Namespace,
                Status = sistema.Status,
                IsActive = sistema.IsActive,
                Version = sistema.Version,
                Description = sistema.Description,
                CreatedAt = sistema.CreatedAt,
                UpdatedAt = sistema.UpdatedAt,
                PublishedAt = sistema.PublishedAt
            };
        }

        public static int? Crear(SistemaCreateRequest request)
        {
            using var context = new SystemBaseContext();

            var slug = request.Slug.Trim().ToLowerInvariant();
            if (!IsValidSlug(slug))
                return null;
            var exists = context.Systems.Any(s => s.Slug == slug);
            if (exists)
                return null;

            var sistema = new Systems
            {
                Slug = slug,
                Name = request.Name.Trim(),
                Namespace = request.Namespace.Trim(),
                Description = request.Description?.Trim(),
                Version = request.Version?.Trim(),
                Status = "draft",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Systems.Add(sistema);
            context.SaveChanges();

            return sistema.Id;
        }

        public static bool Editar(int id, SistemaUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var sistema = context.Systems.FirstOrDefault(s => s.Id == id);
            if (sistema == null)
                return false;

            sistema.Name = request.Name.Trim();
            sistema.Namespace = request.Namespace.Trim();
            sistema.Description = request.Description?.Trim();
            sistema.Version = request.Version?.Trim();
            sistema.IsActive = request.IsActive;
            sistema.UpdatedAt = DateTime.UtcNow;

            context.SaveChanges();
            return true;
        }

        public static (bool Ok, bool NotFound, string? Error, string? SchemaArchivado) Eliminar(int id)
        {
            using var context = new SystemBaseContext();

            var sistema = context.Systems.FirstOrDefault(s => s.Id == id);
            if (sistema == null)
                return (false, true, null, null);

            using var trx = context.Database.BeginTransaction();
            try
            {
                // Los datos no se borran: el schema se archiva con otro nombre y el slug queda libre.
                var schemaArchivado = ArchivarSchema(context, sistema.Slug);

                var entityIds = context.Entities
                    .Where(e => e.SystemId == id)
                    .Select(e => e.Id)
                    .ToList();

                var permissions = context.Permissions
                    .Where(p => p.SystemId == id)
                    .Include(p => p.Role)
                    .ToList();

                foreach (var permission in permissions)
                    permission.Role.Clear();

                var systemMenus = context.SystemMenus
                    .Where(m => m.SystemId == id)
                    .Include(m => m.Role)
                    .ToList();

                foreach (var menu in systemMenus)
                    menu.Role.Clear();

                if (entityIds.Count > 0)
                {
                    var fields = context.Fields
                        .Where(f => entityIds.Contains(f.EntityId));

                    var entityModules = context.EntityModules
                        .Where(em => entityIds.Contains(em.EntityId));

                    context.Fields.RemoveRange(fields);
                    context.EntityModules.RemoveRange(entityModules);
                }

                var relations = context.Relations.Where(r => r.SystemId == id);
                var systemBuilds = context.SystemBuilds.Where(b => b.SystemId == id);
                var systemModules = context.SystemModules.Where(sm => sm.SystemId == id);
                var entities = context.Entities.Where(e => e.SystemId == id);

                context.Relations.RemoveRange(relations);
                context.SystemBuilds.RemoveRange(systemBuilds);
                context.SystemModules.RemoveRange(systemModules);
                context.Permissions.RemoveRange(permissions);
                context.SystemMenus.RemoveRange(systemMenus);
                context.Entities.RemoveRange(entities);
                context.Systems.Remove(sistema);

                context.SaveChanges();
                trx.Commit();

                return (true, false, null, schemaArchivado);
            }
            catch (Exception ex)
            {
                trx.Rollback();
                return (false, false, ex.Message, null);
            }
        }

        /// <summary>
        /// Mueve todas las tablas de sys_&lt;slug&gt; a sys_&lt;slug&gt;_eliminado_&lt;fecha&gt; y borra el schema original.
        /// SQL Server no permite renombrar un schema: se crea el nuevo y se transfiere cada objeto.
        /// Devuelve el nombre del schema archivado, o null si el sistema no estaba publicado.
        /// </summary>
        private static string? ArchivarSchema(SystemBaseContext context, string slug)
        {
            var origen = $"sys_{slug}";
            var destino = $"{origen}_eliminado_{DateTime.UtcNow:yyyyMMddHHmmss}";

            const string sql = @"
DECLARE @origen SYSNAME = @p0, @destino SYSNAME = @p1;
IF SCHEMA_ID(@origen) IS NULL
BEGIN
    SELECT CAST(0 AS BIT) AS Value;
    RETURN;
END
-- EXEC(...) no admite funciones: las sentencias se arman en variables.
DECLARE @crear NVARCHAR(400) = N'CREATE SCHEMA ' + QUOTENAME(@destino);
EXEC sp_executesql @crear;
DECLARE @mover NVARCHAR(MAX) = N'';
SELECT @mover = @mover + N'ALTER SCHEMA ' + QUOTENAME(@destino) + N' TRANSFER ' + QUOTENAME(@origen) + N'.' + QUOTENAME(o.name) + N';'
FROM sys.objects o
WHERE o.schema_id = SCHEMA_ID(@origen) AND o.parent_object_id = 0;
IF LEN(@mover) > 0 EXEC sp_executesql @mover;
DECLARE @borrar NVARCHAR(400) = N'DROP SCHEMA ' + QUOTENAME(@origen);
EXEC sp_executesql @borrar;
SELECT CAST(1 AS BIT) AS Value;";

            var archivado = context.Database
                .SqlQueryRaw<bool>(sql, new SqlParameter("@p0", origen), new SqlParameter("@p1", destino))
                .AsEnumerable()
                .FirstOrDefault();

            return archivado ? destino : null;
        }

        // El slug termina en nombres de schema, carpetas y URLs: solo ASCII en minúsculas,
        // dígitos y guion bajo, empezando con letra. Máximo 60 (deja lugar al sufijo de archivado).
        private static readonly System.Text.RegularExpressions.Regex PatronSlug = new("^[a-z][a-z0-9_]{0,59}$");

        private static bool IsValidSlug(string slug) => PatronSlug.IsMatch(slug);
    }
}
