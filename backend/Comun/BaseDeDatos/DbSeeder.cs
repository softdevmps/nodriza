using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Comun.Seguridad;

namespace Backend.Comun.BaseDeDatos
{
    public static class DbSeeder
    {
        public static void Seed(IServiceProvider services, ILogger logger)
        {
            try
            {
                using var scope = services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SystemBaseContext>();

                if (!context.Database.CanConnect())
                {
                    logger.LogWarning("DbSeeder: no se pudo conectar a la base de datos. Se omite el seed.");
                    return;
                }

                EnsureModules(context, logger);
                var adminRole = EnsureAdminRole(context, logger);
                var adminUser = EnsureAdminUser(context, adminRole, logger);
                var baseMenus = EnsureBaseMenus(context, logger);
                EnsureRoleMenus(context, adminRole, baseMenus, logger);

                if (adminUser != null && adminUser.RolId != adminRole.Id)
                {
                    adminUser.RolId = adminRole.Id;
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DbSeeder: error inesperado durante el seed.");
            }
        }

        private static void EnsureModules(SystemBaseContext context, ILogger logger)
        {
            var needed = new[]
            {
                new { Name = "backend", Description = "Generador de backend", Version = "1.0" },
                new { Name = "frontend", Description = "Generador de frontend", Version = "1.0" }
            };

            var existing = context.Modules
                .Select(m => m.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = false;
            foreach (var module in needed)
            {
                if (existing.Contains(module.Name))
                    continue;

                context.Modules.Add(new Modules
                {
                    Name = module.Name,
                    Description = module.Description,
                    Version = module.Version
                });
                added = true;
            }

            if (added)
            {
                context.SaveChanges();
                logger.LogInformation("DbSeeder: modulos base creados.");
            }
        }

        private static Roles EnsureAdminRole(SystemBaseContext context, ILogger logger)
        {
            var role = context.Roles.FirstOrDefault(r => r.Nombre.ToLower() == "admin");
            if (role != null)
                return role;

            role = new Roles
            {
                Nombre = "Admin",
                Activo = true
            };

            context.Roles.Add(role);
            context.SaveChanges();
            logger.LogInformation("DbSeeder: rol Admin creado.");
            return role;
        }

        private static Usuarios? EnsureAdminUser(SystemBaseContext context, Roles adminRole, ILogger logger)
        {
            var user = context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefault(u => u.Username == "admin" || u.Email == "admin@local");

            if (user != null)
                return user;

            // Nunca admin/admin: la contraseña sale de ADMIN_PASSWORD. Si no está definida se genera
            // una aleatoria y se muestra UNA sola vez en la consola (solo en este primer arranque).
            var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            var generada = string.IsNullOrWhiteSpace(password);
            if (generada)
            {
                password = GenerarContrasena();
            }
            else if (PoliticaContrasenas.Validar(password) is { } motivo)
            {
                logger.LogError("DbSeeder: ADMIN_PASSWORD no cumple la política ({Motivo}). No se creó el usuario admin.", motivo);
                return null;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            user = new Usuarios
            {
                Username = "admin",
                Email = "admin@local",
                PasswordHash = hash,
                Nombre = "Admin",
                Apellido = "System",
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                RolId = adminRole.Id
            };

            context.Usuarios.Add(user);
            context.SaveChanges();
            if (generada)
            {
                Console.WriteLine();
                Console.WriteLine("==============================================================");
                Console.WriteLine(" Usuario administrador creado: admin");
                Console.WriteLine($" Contraseña inicial (se muestra solo esta vez): {password}");
                Console.WriteLine(" Cambiala desde Usuarios o definí ADMIN_PASSWORD en el .env.");
                Console.WriteLine("==============================================================");
                Console.WriteLine();
            }
            logger.LogInformation("DbSeeder: usuario admin creado.");
            return user;
        }

        private static string GenerarContrasena()
        {
            const string letras = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digitos = "23456789";
            var todos = letras + digitos;
            var chars = new char[20];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = todos[System.Security.Cryptography.RandomNumberGenerator.GetInt32(todos.Length)];
            // Garantiza letras y números (política de contraseñas)
            chars[0] = letras[System.Security.Cryptography.RandomNumberGenerator.GetInt32(letras.Length)];
            chars[1] = digitos[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digitos.Length)];
            return new string(chars);
        }

        private static List<Menus> EnsureBaseMenus(SystemBaseContext context, ILogger logger)
        {
            var menus = context.Menus.ToList();
            Menus? sistemaMenu = menus.FirstOrDefault(m => m.Titulo == "Sistema" && m.PadreId == null);
            if (sistemaMenu == null)
            {
                sistemaMenu = new Menus
                {
                    Titulo = "Sistema",
                    Ruta = null,
                    Icono = "mdi-cog",
                    Orden = 2,
                    PadreId = null,
                    Activo = true
                };
                context.Menus.Add(sistemaMenu);
                context.SaveChanges();
                menus.Add(sistemaMenu);
            }

            var baseItems = new[]
            {
                new { Title = "Home", Route = "/home", Icon = "mdi-home", Order = 1, ParentId = (int?)null },
                new { Title = "Sistemas", Route = "/sistemas", Icon = "mdi-apps", Order = 1, ParentId = (int?)sistemaMenu.Id },
                new { Title = "Menu", Route = "/menu", Icon = "mdi-view-list", Order = 2, ParentId = (int?)sistemaMenu.Id },
                new { Title = "Usuarios", Route = "/usuarios", Icon = "mdi-account-group", Order = 3, ParentId = (int?)sistemaMenu.Id },
                new { Title = "Roles", Route = "/roles", Icon = "mdi-shield-account", Order = 4, ParentId = (int?)sistemaMenu.Id }
            };

            var created = false;
            foreach (var item in baseItems)
            {
                var exists = menus.Any(m => m.Titulo == item.Title && m.PadreId == item.ParentId);
                if (exists)
                    continue;

                var menu = new Menus
                {
                    Titulo = item.Title,
                    Ruta = item.Route,
                    Icono = item.Icon,
                    Orden = item.Order,
                    PadreId = item.ParentId,
                    Activo = true
                };

                context.Menus.Add(menu);
                menus.Add(menu);
                created = true;
            }

            if (created)
            {
                context.SaveChanges();
                logger.LogInformation("DbSeeder: menus base creados.");
            }

            return menus.Where(m => m.Titulo is "Home" or "Sistema" or "Sistemas" or "Menu" or "Usuarios" or "Roles").ToList();
        }

        private static void EnsureRoleMenus(SystemBaseContext context, Roles role, List<Menus> menus, ILogger logger)
        {
            context.Entry(role).Collection(r => r.Menu).Load();
            var assigned = role.Menu.Select(m => m.Id).ToHashSet();

            var added = false;
            foreach (var menu in menus)
            {
                if (assigned.Contains(menu.Id))
                    continue;
                role.Menu.Add(menu);
                added = true;
            }

            if (added)
            {
                context.SaveChanges();
                logger.LogInformation("DbSeeder: menus asignados al rol Admin.");
            }
        }
    }
}
