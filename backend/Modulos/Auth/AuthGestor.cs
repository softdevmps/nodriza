using BCrypt.Net;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
// Alias: el namespace Backend.Modulos.Usuarios/Roles tapa a las clases de tabla con el mismo nombre
using Tablas = Backend.Comun.BaseDeDatos.Tablas;
using Backend.Comun.Seguridad;
using Backend.Modulos.Auth.Modelos;

namespace Backend.Modulos.Auth
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

            var nuevoUsuario = new Tablas.Usuarios
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