using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;

namespace Backend.Comun.Seguridad
{
    /// <summary>
    /// Política "Admin": el usuario del token debe estar activo y tener el rol Admin activo.
    /// Se consulta la base en cada request (no un claim del token): si se le quita el rol
    /// o se desactiva, pierde el acceso en el momento, sin esperar a que venza el token.
    /// Uso: [Authorize(Policy = Politicas.Admin)]
    /// </summary>
    public static class Politicas
    {
        public const string Admin = "Admin";
        public const string NombreRolAdmin = "admin";
    }

    public class RequiereAdmin : IAuthorizationRequirement
    {
    }

    public class RequiereAdminHandler : AuthorizationHandler<RequiereAdmin>
    {
        private readonly SystemBaseContext _context;

        public RequiereAdminHandler(SystemBaseContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequiereAdmin requirement)
        {
            if (!int.TryParse(context.User.FindFirst("usuarioId")?.Value, out var usuarioId) || usuarioId <= 0)
                return;

            var esAdmin = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id == usuarioId &&
                    u.Activo &&
                    u.Rol != null &&
                    u.Rol.Activo &&
                    u.Rol.Nombre.ToLower() == Politicas.NombreRolAdmin);

            if (esAdmin)
                context.Succeed(requirement);
        }
    }
}
