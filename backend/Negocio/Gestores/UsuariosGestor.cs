using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
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

        public static bool Crear(UsuarioCreateRequest request)
        {
            using var context = new SystemBaseContext();

            var usuario = new Usuarios
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                RolId = request.RolId,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            context.Usuarios.Add(usuario);
            context.SaveChanges();

            return true;
        }

        public static bool Editar(int id, UsuarioUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var usuario = context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null)
                return false;

            usuario.Username = request.Username;
            usuario.Email = request.Email;
            usuario.Nombre = request.Nombre;
            usuario.Apellido = request.Apellido;
            usuario.RolId = request.RolId;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            context.SaveChanges();
            return true;
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
