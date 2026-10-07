using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Requests.Roles;
using Backend.Models.Responses.Roles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class RolesGestor
    {

        public static List<RolResponse> ObtenerTodos()
        {
            using var context = new SystemBaseContext();

            return context.Roles
            .OrderBy(r => r.Id)
            .Select(r => new RolResponse
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Activo = r.Activo
            })
            .ToList();
        }

        public static RolDetalleResponse? ObtenerPorId(int id)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles
                .Include(r => r.Menu)
                .FirstOrDefault(r => r.Id == id);

            if (rol == null)
                return null;

            var menusAsignadosIds = rol.Menu
                .Select(m => m.Id)
                .ToHashSet();

            var menus = context.Menus
                .Where(m => m.Activo)
                .Select(m => new RolMenuResponse
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Icono = m.Icono,
                    Ruta = m.Ruta,
                    Asignado = menusAsignadosIds.Contains(m.Id)
                })
                .ToList();

            return new RolDetalleResponse
            {
                Id = rol.Id,
                Nombre = rol.Nombre,
                Activo = rol.Activo,
                Menus = menus
            };
        }

        public static bool Crear(RolCreateRequest request)
        {
            using var context = new SystemBaseContext();

            var rol = new Roles
            {
                Nombre = request.Nombre,
                Activo = request.Activo
            };

            context.Roles.Add(rol);
            context.SaveChanges();

            return true;
        }

        public static bool Editar(int id, RolUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
                return false;

            rol.Nombre = request.Nombre;
            rol.Activo = request.Activo;

            context.SaveChanges();
            return true;
        }

        public static bool CambiarEstado(int id, bool activo)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
                return false;

            rol.Activo = activo;
            context.SaveChanges();

            return true;
        }

        public static bool AsignarMenus(int rolId, List<int> menusIds)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles
                .Include(r => r.Menu)
                .FirstOrDefault(r => r.Id == rolId);

            if (rol == null)
                return false;

            rol.Menu.Clear();

            var menus = context.Menus
                .Where(m => menusIds.Contains(m.Id))
                .ToList();

            foreach (var menu in menus)
                rol.Menu.Add(menu);

            context.SaveChanges();
            return true;
        }

        public static List<RolSystemMenuResponse>? ObtenerSystemMenusPorRol(int rolId)
        {
            using var context = new SystemBaseContext();

            var rolExists = context.Roles.Any(r => r.Id == rolId);
            if (!rolExists)
                return null;

            var assignedSystemIds = context.SystemMenus
                .Where(m => m.Role.Any(r => r.Id == rolId))
                .Select(m => m.SystemId)
                .Distinct()
                .ToHashSet();

            var systems = context.Systems
                .Where(s => s.IsActive && s.Status == "published")
                .OrderBy(s => s.Name)
                .Select(s => new RolSystemMenuResponse
                {
                    SystemId = s.Id,
                    SystemName = s.Name,
                    SystemSlug = s.Slug,
                    Asignado = assignedSystemIds.Contains(s.Id)
                })
                .ToList();

            return systems;
        }

        public static bool AsignarSystemMenus(int rolId, List<int> systemIds)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles
                .Include(r => r.SystemMenu)
                .FirstOrDefault(r => r.Id == rolId);

            if (rol == null)
                return false;

            rol.SystemMenu.Clear();

            if (systemIds.Count > 0)
            {
                var menus = context.SystemMenus
                    .Where(m => systemIds.Contains(m.SystemId) && m.IsActive)
                    .ToList();

                foreach (var menu in menus)
                    rol.SystemMenu.Add(menu);
            }

            context.SaveChanges();
            return true;
        }

        public static List<RolPermissionResponse>? ObtenerPermisosPorRol(int rolId, int systemId)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles
                .Include(r => r.Permission)
                .FirstOrDefault(r => r.Id == rolId);

            if (rol == null)
                return null;

            var assignedIds = rol.Permission
                .Where(p => p.SystemId == systemId)
                .Select(p => p.Id)
                .ToHashSet();

            var permissions = context.Permissions
                .Where(p => p.SystemId == systemId)
                .ToList();

            var entityMap = context.Entities
                .Where(e => e.SystemId == systemId)
                .ToDictionary(e => e.Id, e => e.DisplayName ?? e.Name);

            var result = new List<RolPermissionResponse>();

            foreach (var perm in permissions)
            {
                if (!PermisosGestor.TryParseKey(perm.Key, out var entityId, out var action))
                    continue;

                if (!entityMap.TryGetValue(entityId, out var entityName))
                    continue;

                result.Add(new RolPermissionResponse
                {
                    PermissionId = perm.Id,
                    EntityId = entityId,
                    EntityName = entityName,
                    Action = action,
                    Asignado = assignedIds.Contains(perm.Id)
                });
            }

            return result
                .OrderBy(r => r.EntityName)
                .ThenBy(r => r.Action)
                .ToList();
        }

        public static bool AsignarPermisos(int rolId, int systemId, List<int> permissionIds)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles
                .Include(r => r.Permission)
                .FirstOrDefault(r => r.Id == rolId);

            if (rol == null)
                return false;

            var current = rol.Permission
                .Where(p => p.SystemId == systemId)
                .ToList();

            foreach (var perm in current)
                rol.Permission.Remove(perm);

            if (permissionIds.Count > 0)
            {
                var toAdd = context.Permissions
                    .Where(p => p.SystemId == systemId && permissionIds.Contains(p.Id))
                    .ToList();

                foreach (var perm in toAdd)
                    rol.Permission.Add(perm);
            }

            context.SaveChanges();
            return true;
        }
    }
}
