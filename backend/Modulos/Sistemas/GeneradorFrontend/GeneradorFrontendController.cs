using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.Seguridad;

namespace Backend.Modulos.Sistemas.GeneradorFrontend
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
    public class GeneradorFrontendController : AppController
    {
        private readonly IWebHostEnvironment _env;

        public GeneradorFrontendController(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>Copia frontend-runtime a systems/&lt;slug&gt;/frontend apuntando al backend del sistema.</summary>
        [HttpPost(Routes.v1.Sistemas.GenerarFrontend)]
        public IActionResult GenerarFrontend(int id, [FromQuery] bool overwrite = false)
        {
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var frontendSource = Path.Combine(repoRoot, "frontend-runtime");
            var outputRoot = Path.Combine(repoRoot, "systems", sistema.Slug, "frontend");

            var result = SistemasFrontendGenerator.Generar(id, frontendSource, outputRoot, overwrite);
            if (!result.Ok)
                return BadRequest(result);

            FrontendConfigGestor.HabilitarFrontend(id);
            return Ok(result);
        }
    }
}
