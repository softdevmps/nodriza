using Backend.Comun.Seguridad;
using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
// Alias: el namespace Backend.Modulos.Usuarios/Roles tapa a las clases de tabla con el mismo nombre
using Tablas = Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Usuarios.Modelos;

namespace Backend.Modulos.Usuarios
{
    public static class UsuariosGestor
    {
        public static List<UsuarioResponse> ObtenerTodos()
        {
            using var context = new SystemBaseContext();

            return context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.Id)
               .Select(u => new UsuarioResponse
               {
                   Id = u.Id,
                   Username = u.Username,
                   Email = u.Email,
                   NombreCompleto = u.Nombre + " " + u.Apellido,
                   RolId = u.RolId,              // 🔑
                   Rol = u.Rol != null ? u.Rol.Nombre : "",

                   Activo = u.Activo
               })
                .ToList();
        }

        public static UsuarioDetalleResponse? ObtenerPorId(int id)
        {
            using var context = new SystemBaseContext();

            var usuario = context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefault(u => u.Id == id);

            if (usuario == null)
                return null;

            return new UsuarioDetalleResponse
            {
                Id = usuario.Id,
                Username = usuario.Username,
                Email = usuario.Email,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                RolId = usuario.RolId,
                Rol = usuario.Rol != null ? usuario.Rol.Nombre : "",
                Activo = usuario.Activo
            };
        }

        public static (bool Ok, string? Error) Crear(UsuarioCreateRequest request)
        {
            using var context = new SystemBaseContext();

            var error = PoliticaContrasenas.Validar(request.Password) ?? Validar(context, null, request.Username, request.Email, request.RolId);
            if (error != null)
                return (false, error);

            var usuario = new Tablas.Usuarios
            {
                Username = request.Username.Trim(),
                Email = request.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Nombre = request.Nombre.Trim(),
                Apellido = request.Apellido.Trim(),
                RolId = request.RolId,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            context.Usuarios.Add(usuario);
            context.SaveChanges();

            return (true, null);
        }

        public static (bool Ok, bool NotFound, string? Error) Editar(int id, UsuarioUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var usuario = context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null)
                return (false, true, null);

            var error = Validar(context, id, request.Username, request.Email, request.RolId);
            if (error == null && !string.IsNullOrWhiteSpace(request.Password))
                error = PoliticaContrasenas.Validar(request.Password);
            if (error != null)
                return (false, false, error);

            usuario.Username = request.Username.Trim();
            usuario.Email = request.Email.Trim();
            usuario.Nombre = request.Nombre.Trim();
            usuario.Apellido = request.Apellido.Trim();
            usuario.RolId = request.RolId;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            context.SaveChanges();
            return (true, false, null);
        }

        /// <summary>Username y email únicos (ignorando al propio usuario) y rol existente.</summary>
        private static string? Validar(SystemBaseContext context, int? idPropio, string username, string email, int rolId)
        {
            username = username.Trim();
            email = email.Trim();
            if (context.Usuarios.Any(u => u.Id != idPropio && u.Username == username))
                return "Ya existe un usuario con ese nombre de usuario.";
            if (context.Usuarios.Any(u => u.Id != idPropio && u.Email == email))
                return "Ya existe un usuario con ese email.";
            if (!context.Roles.Any(r => r.Id == rolId))
                return "El rol elegido no existe.";
            return null;
        }

        public static bool CambiarEstado(int id, bool activo)
        {
            using var context = new SystemBaseContext();

            var usuario = context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null)
                return false;

            usuario.Activo = activo;
            context.SaveChanges();

            return true;
        }
    }
}
