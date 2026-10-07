using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
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

        public static (bool Ok, bool NotFound, string? Error) Eliminar(int id)
        {
            using var context = new SystemBaseContext();

            var sistema = context.Systems.FirstOrDefault(s => s.Id == id);
            if (sistema == null)
                return (false, true, null);

            using var trx = context.Database.BeginTransaction();
            try
            {
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

                return (true, false, null);
            }
            catch (Exception ex)
            {
                trx.Rollback();
                return (false, false, ex.Message);
            }
        }

        private static bool IsValidSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            if (char.IsDigit(slug[0]))
                return false;

            foreach (var ch in slug)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                    return false;
            }

            return true;
        }
    }
}
