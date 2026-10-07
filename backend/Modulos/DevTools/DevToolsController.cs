using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Backend.Comun;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Comun.Seguridad;

namespace Backend.Modulos.DevTools
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
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

            _ = Task.Run(() => _lifetime.StopApplication());

            return Ok(new { message = "Reiniciando backend..." });
        }
    }
}
