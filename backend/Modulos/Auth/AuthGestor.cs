using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
// Alias: el namespace Backend.Modulos.Usuarios/Roles tapa a las clases de tabla con el mismo nombre
using Tablas = Backend.Comun.BaseDeDatos.Tablas;
using Backend.Comun.Seguridad;
using Backend.Modulos.Auth.Modelos;

namespace Backend.Modulos.Auth
{
    public class AuthGestor
    {
        private readonly IDbContextFactory<SystemBaseContext> _contextos;

        public AuthGestor(IDbContextFactory<SystemBaseContext> contextos)
        {
            _contextos = contextos;
        }

        public LoginResponse? Login(LoginRequest request)
        {
            using var context = _contextos.CreateDbContext();

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

        /// <summary>Devuelve null si se registró; si no, el motivo para el usuario.</summary>
        public string? Registrar(RegistrarRequest model)
        {
            using var context = _contextos.CreateDbContext();

            if (PoliticaContrasenas.Validar(model.Password) is { } motivo)
                return motivo;

            if (context.Usuarios.Any(u =>
                u.Username == model.Username || u.Email == model.Email))
                return "Usuario o email ya existente";

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

            return null;
        }
    }
}