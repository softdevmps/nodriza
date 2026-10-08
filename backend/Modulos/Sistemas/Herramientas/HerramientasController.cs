using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.Seguridad;

namespace Backend.Modulos.Sistemas.Herramientas
{
    /// <summary>
    /// Pestaña "Herramientas" del diseñador (solo DEV): iniciar/detener el backend y el frontend
    /// generados, ver sus logs y si están online. La lógica está en ProcesosSistemas.
    /// </summary>
    [ApiController]
    [Authorize]
    public class HerramientasController : AppController
    {
        private readonly ProcesosSistemas _procesosSistemas;
        private readonly SistemasGestor _sistemasGestor;
        private readonly IWebHostEnvironment _env;

        public HerramientasController(IWebHostEnvironment env, ProcesosSistemas procesosSistemas, SistemasGestor sistemasGestor)
        {
            _procesosSistemas = procesosSistemas;
            _sistemasGestor = sistemasGestor;
            _env = env;
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPost(Routes.v1.Sistemas.IniciarBackend)]
        public Task<IActionResult> IniciarBackend(int id) => Iniciar(Componente.Backend, id);

        [Authorize(Policy = Politicas.Admin)]
        [HttpPost(Routes.v1.Sistemas.IniciarFrontend)]
        public Task<IActionResult> IniciarFrontend(int id) => Iniciar(Componente.Frontend, id);

        [Authorize(Policy = Politicas.Admin)]
        [HttpPost(Routes.v1.Sistemas.DetenerBackend)]
        public IActionResult DetenerBackend(int id) => Detener(Componente.Backend, id);

        [Authorize(Policy = Politicas.Admin)]
        [HttpPost(Routes.v1.Sistemas.DetenerFrontend)]
        public IActionResult DetenerFrontend(int id) => Detener(Componente.Frontend, id);

        // Los ping solo informan {online}: los usa también la vista embebida del frontend.
        [HttpGet(Routes.v1.Sistemas.PingBackend)]
        public Task<IActionResult> PingBackend(int id) => Ping(Componente.Backend, id);

        [HttpGet(Routes.v1.Sistemas.PingFrontend)]
        public Task<IActionResult> PingFrontend(int id) => Ping(Componente.Frontend, id);

        [Authorize(Policy = Politicas.Admin)]
        [HttpGet(Routes.v1.Sistemas.LogsBackend)]
        public IActionResult LogsBackend(int id, [FromQuery] long after = 0, [FromQuery] int take = 200) =>
            Logs(Componente.Backend, id, after, take);

        [Authorize(Policy = Politicas.Admin)]
        [HttpGet(Routes.v1.Sistemas.LogsFrontend)]
        public IActionResult LogsFrontend(int id, [FromQuery] long after = 0, [FromQuery] int take = 200) =>
            Logs(Componente.Frontend, id, after, take);

        private async Task<IActionResult> Iniciar(Componente componente, int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var sistema = _sistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var carpeta = Path.Combine(repoRoot, "systems", sistema.Slug, componente == Componente.Backend ? "backend" : "frontend");
            var r = await _procesosSistemas.IniciarAsync(componente, id, carpeta);
            if (!r.Ok)
                return BadRequest(new { message = r.Message });

            return r.Port is { } port
                ? Ok(new { status = r.Status, message = r.Message, port })
                : Ok(new { status = r.Status, message = r.Message });
        }

        private IActionResult Detener(Componente componente, int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var r = _procesosSistemas.Detener(componente, id);
            return Ok(new { status = r.Status, message = r.Message });
        }

        private async Task<IActionResult> Ping(Componente componente, int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            return Ok(new { online = await _procesosSistemas.EstaOnlineAsync(componente, id) });
        }

        private IActionResult Logs(Componente componente, int id, long after, int take)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var items = LogsProcesos.Get(componente, id).Read(after, Math.Clamp(take, 1, 500), out var lastId);
            return Ok(new { items, lastId });
        }
    }
}
