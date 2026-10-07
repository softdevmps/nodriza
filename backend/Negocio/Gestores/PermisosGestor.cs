using Backend.Data;
using Backend.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class PermisosGestor
    {
        public static readonly string[] Actions = { "view", "create", "edit", "delete" };

        public static string BuildKey(int entityId, string action)
        {
            var normalized = action?.Trim().ToLowerInvariant() ?? "view";
            return $"entity:{entityId}:{normalized}";
        }

        public static bool TryParseKey(string key, out int entityId, out string action)
        {
            entityId = 0;
            action = string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            var parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
                return false;

            if (!parts[0].Equals("entity", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!int.TryParse(parts[1], out entityId))
                return false;

            var normalized = parts[2].ToLowerInvariant();
            if (!Actions.Contains(normalized))
                return false;

            action = normalized;
            return true;
        }

        public static bool SystemHasPermissions(SystemBaseContext context, int systemId)
        {
            return context.Permissions.Any(p => p.SystemId == systemId);
        }

        public static bool UsuarioTienePermiso(SystemBaseContext context, int usuarioId, int systemId, int entityId, string action)
        {
            if (usuarioId == 0)
                return false;

            if (!SystemHasPermissions(context, systemId))
                return true;

            var user = context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
            if (user?.RolId == null)
                return false;

            var key = BuildKey(entityId, action);
            var permissionId = context.Permissions
                .Where(p => p.SystemId == systemId && p.Key == key)
                .Select(p => p.Id)
                .FirstOrDefault();

            if (permissionId == 0)
                return false;

            return context.Roles
                .Where(r => r.Id == user.RolId)
                .Any(r => r.Permission.Any(p => p.Id == permissionId));
        }

        public static HashSet<int> ObtenerEntidadesPermitidas(SystemBaseContext context, int usuarioId, int systemId, string action)
        {
            var entities = context.Entities
                .Where(e => e.SystemId == systemId)
                .Select(e => e.Id)
                .ToHashSet();

            if (!SystemHasPermissions(context, systemId))
                return entities;

            if (usuarioId == 0)
                return new HashSet<int>();

            var user = context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
            if (user?.RolId == null)
                return new HashSet<int>();

            var keys = context.Roles
                .Where(r => r.Id == user.RolId)
                .SelectMany(r => r.Permission)
                .Where(p => p.SystemId == systemId)
                .Select(p => p.Key)
                .ToList();

            var allowed = new HashSet<int>();
            foreach (var key in keys)
            {
                if (TryParseKey(key, out var entityId, out var parsedAction) && parsedAction == action)
                {
                    allowed.Add(entityId);
                }
            }

            return allowed;
        }
    }
}
