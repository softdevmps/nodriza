using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Menu.Modelos;

namespace Backend.Modulos.Menu
{
    public class MenuGestor
    {
        private readonly IDbContextFactory<SystemBaseContext> _contextos;

        public MenuGestor(IDbContextFactory<SystemBaseContext> contextos)
        {
            _contextos = contextos;
        }

        public List<MenuItemResponse> ObtenerMenuPorUsuario(int usuarioId)
        {
            using var context = _contextos.CreateDbContext();

            var menus = context.Menus
                .Where(m =>
                    m.Activo &&
                    m.Rol.Any(r =>
                        r.Activo && r.Usuarios.Any(u => u.Id == usuarioId)
                    )
                )
                .OrderBy(m => m.Orden)
                .Select(m => new MenuItemResponse
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Icono = m.Icono,
                    Ruta = m.Ruta,
                    Orden = m.Orden,
                    PadreId = m.PadreId
                })
                .ToList();

            return menus;
        }

        public bool Crear(MenuRequest request)
        {
            using var context = _contextos.CreateDbContext();

            var ruta = MenuViewGenerator.NormalizeRoute(request.Ruta);

            if (request.PadreId != null && string.IsNullOrWhiteSpace(ruta))
            {
                var parentTitle = context.Menus
                    .Where(m => m.Id == request.PadreId)
                    .Select(m => m.Titulo)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(parentTitle))
                {
                    var parentSlug = MenuViewGenerator.ToSlug(parentTitle);
                    var childSlug = MenuViewGenerator.ToSlug(request.Titulo);
                    ruta = $"/{parentSlug}/{childSlug}";
                }
                else
                {
                    var childSlug = MenuViewGenerator.ToSlug(request.Titulo);
                    ruta = $"/{childSlug}";
                }
            }

            var menu = new Menus
            {
                Titulo = request.Titulo,
                Icono = request.Icono,
                Ruta = ruta,
                Orden = request.Orden,
                PadreId = request.PadreId,
                Activo = true
            };

            var roles = context.Roles
                .Where(r => request.RolesIds.Contains(r.Id))
                .ToList();

            foreach (var rol in roles)
                menu.Rol.Add(rol);

            context.Menus.Add(menu);
            context.SaveChanges();

            if (menu.PadreId == null)
            {
                MenuViewGenerator.TryEnsureParentFolder(menu.Titulo);
            }
            else
            {
                var parentTitle = context.Menus
                    .Where(m => m.Id == menu.PadreId)
                    .Select(m => m.Titulo)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(parentTitle))
                    MenuViewGenerator.TryCreateChildView(parentTitle, menu.Titulo);
            }

            return true;
        }

        public bool Editar(int id, MenuRequest request)
        {
            using var context = _contextos.CreateDbContext();

            var menu = context.Menus
                .Include(m => m.Rol)
                .FirstOrDefault(m => m.Id == id);

            if (menu == null)
                return false;

            menu.Titulo = request.Titulo;
            menu.Icono = request.Icono;
            menu.Ruta = string.IsNullOrWhiteSpace(request.Ruta)
                ? menu.Ruta
                : MenuViewGenerator.NormalizeRoute(request.Ruta);
            menu.Orden = request.Orden;
            menu.PadreId = request.PadreId;

            menu.Rol.Clear();

            var roles = context.Roles
                .Where(r => request.RolesIds.Contains(r.Id))
                .ToList();

            foreach (var rol in roles)
                menu.Rol.Add(rol);

            context.SaveChanges();
            return true;
        }

        public bool Desactivar(int id)
        {
            using var context = _contextos.CreateDbContext();

            var menu = context.Menus.FirstOrDefault(m => m.Id == id);
            if (menu == null)
                return false;

            menu.Activo = false;
            context.SaveChanges();
            return true;
        }

        public List<MenuTreeResponse> ObtenerMenuTreePorUsuario(int usuarioId)
        {
            using var context = _contextos.CreateDbContext();

            // 1️⃣ Traemos TODOS los menús permitidos (plano)
            var menus = context.Menus
                .Where(m =>
                    m.Activo &&
                    m.Rol.Any(r =>
                        r.Activo && r.Usuarios.Any(u => u.Id == usuarioId)
                    )
                )
                .OrderBy(m => m.Orden)
                .Select(m => new
                {
                    m.Id,
                    m.Titulo,
                    m.Icono,
                    m.Ruta,
                    m.PadreId,
                    m.Orden
                })
                .ToList();

            // 2️⃣ Diccionario para lookup rápido
            var lookup = menus.ToDictionary(
                m => m.Id,
                m => new MenuTreeResponse
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Icono = m.Icono,
                    Ruta = m.Ruta,
                    Orden = m.Orden
                }
            );
            // 3️⃣ Armar el árbol
            List<MenuTreeResponse> root = new();

            foreach (var m in menus)
            {
                if (m.PadreId == null)
                {
                    root.Add(lookup[m.Id]);
                }
                else if (lookup.ContainsKey(m.PadreId.Value))
                {
                    lookup[m.PadreId.Value]
                        .Children
                        .Add(lookup[m.Id]);
                }
            }
            // Ordenar recursivamente
            void Ordenar(List<MenuTreeResponse> items)
            {
                items.Sort((a, b) => a.Orden.CompareTo(b.Orden));
                foreach (var item in items)
                {
                    Ordenar(item.Children);
                }
            }

            Ordenar(root);

            return root;
        }

        public List<MenuTreeResponse> ObtenerSidebarTreePorUsuario(int usuarioId)
        {
            using var context = _contextos.CreateDbContext();

            var baseMenu = ObtenerMenuTreePorUsuario(usuarioId);

            var systemMenus = context.SystemMenus
                .Include(m => m.System)
                .Where(m => m.IsActive)
                .Where(m => m.System.IsActive && m.System.Status == "published")
                .Where(m =>
                    !m.Role.Any() ||
                    m.Role.Any(r => r.Activo && r.Usuarios.Any(u => u.Id == usuarioId))
                )
                .Select(m => new
                {
                    m.SystemId,
                    SystemName = m.System.Name,
                    SystemSlug = m.System.Slug
                })
                .Distinct()
                .ToList();

            var frontendModuleId = context.Modules
                .Where(m => m.Name == "frontend")
                .Select(m => m.Id)
                .FirstOrDefault();

            var frontendEnabled = frontendModuleId == 0
                ? new HashSet<int>()
                : context.SystemModules
                    .Where(sm => sm.ModuleId == frontendModuleId && sm.IsEnabled)
                    .Select(sm => sm.SystemId)
                    .ToHashSet();

            var systemGroups = systemMenus
                .GroupBy(m => new { m.SystemId, m.SystemName, m.SystemSlug })
                .ToList();

            foreach (var group in systemGroups)
            {
                var systemRoot = new MenuTreeResponse
                {
                    Id = -100000 - group.Key.SystemId,
                    Titulo = group.Key.SystemName,
                    Icono = "mdi-apps",
                    Ruta = $"/s/{group.Key.SystemSlug}",
                    Orden = 1000 + group.Key.SystemId
                };

                if (frontendEnabled.Contains(group.Key.SystemId))
                {
                    systemRoot.Children.Add(new MenuTreeResponse
                    {
                        Id = -300000 - group.Key.SystemId,
                        Titulo = "Frontend",
                        Icono = "mdi-monitor",
                        Ruta = $"/frontend/{group.Key.SystemSlug}",
                        Orden = 2
                    });
                }
                else
                {
                    var entidadesGroup = new MenuTreeResponse
                    {
                        Id = -200000 - group.Key.SystemId,
                        Titulo = "Entidades",
                        Icono = "mdi-database",
                        Ruta = $"/s/{group.Key.SystemSlug}",
                        Orden = 1
                    };

                    systemRoot.Children.Add(entidadesGroup);
                }

                baseMenu.Add(systemRoot);
            }

            baseMenu.Sort((a, b) => a.Orden.CompareTo(b.Orden));
            return baseMenu;
        }

    }
}
