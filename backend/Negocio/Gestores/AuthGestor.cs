using Backend.Data;
using Backend.Models.Auth;
using Backend.Models.Jwt;
using BCrypt.Net;

namespace Backend.Negocio.Gestores
{
    public static class AuthGestor
    {
        public static LoginResponse? Login(LoginRequest request)
        {
            using var context = new SystemBaseContext();

            var usuario = context.Usuarios
                .FirstOrDefault(u =>
                    u.Activo &&
                    (u.Username == request.Usuario || u.Email == request.Usuario)
                );

            if (usuario == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
                return null;


            var (token, expiracion) = JwtService.GenerarToken(
                usuario.Id,
                usuario.Username
            );

            return new LoginResponse
            {
                UsuarioId = usuario.Id,
                Usuario = usuario.Username,
                Token = token,
                Expiracion = expiracion
            };
        }

        public static bool Registrar(RegistrarRequest model)
        {
            using var context = new SystemBaseContext();

            // validar duplicados
            if (context.Usuarios.Any(u =>
                u.Username == model.Username || u.Email == model.Email))
                return false;

            var hash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var nuevoUsuario = new Models.Entidades.Usuarios
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = hash,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            context.Usuarios.Add(nuevoUsuario);
            context.SaveChanges();

            return true;
        }
    }
}