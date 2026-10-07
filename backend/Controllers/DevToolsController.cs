using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class DevToolsController : AppController
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IWebHostEnvironment _env;

        public DevToolsController(IHostApplicationLifetime lifetime, IWebHostEnvironment env)
        {
            _lifetime = lifetime;
            _env = env;
        }

        [HttpPost(Routes.v1.DevTools.Restart)]
        public IActionResult Restart()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            using var context = new SystemBaseContext();
            var isAdmin = context.Usuarios
                .Include(u => u.Rol)
                .Any(u =>
                    u.Id == usuario.UsuarioId &&
                    (u.Username.ToLower() == "admin" || (u.Rol != null && u.Rol.Nombre.ToLower() == "admin"))
                );

            if (!isAdmin)
                return Forbid();

            _ = Task.Run(() => _lifetime.StopApplication());

            return Ok(new { message = "Reiniciando backend..." });
        }
    }
}
