using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.Seguridad;
using Backend.Modulos.Sistemas.Herramientas;

namespace Backend.Modulos.Sistemas.GeneradorBackend
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
    public class GeneradorBackendController : AppController
    {
        private readonly ProcesosSistemas _procesosSistemas;
        private readonly SistemasBackendGenerator _sistemasBackendGenerator;
        private readonly SistemasGestor _sistemasGestor;
        private readonly IWebHostEnvironment _env;

        public GeneradorBackendController(IWebHostEnvironment env, ProcesosSistemas procesosSistemas, SistemasBackendGenerator sistemasBackendGenerator, SistemasGestor sistemasGestor)
        {
            _procesosSistemas = procesosSistemas;
            _sistemasBackendGenerator = sistemasBackendGenerator;
            _sistemasGestor = sistemasGestor;
            _env = env;
        }

        /// <summary>Genera el proyecto .NET del sistema en systems/&lt;slug&gt;/backend y hace dotnet restore.</summary>
        [HttpPost(Routes.v1.Sistemas.GenerarBackend)]
        public IActionResult GenerarBackend(int id, [FromQuery] bool overwrite = false)
        {
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = _sistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var outputRoot = Path.Combine(repoRoot, "systems", sistema.Slug, "backend");
            var result = _sistemasBackendGenerator.Generar(id, outputRoot, overwrite);

            if (!result.Ok || string.IsNullOrWhiteSpace(result.OutputPath))
                return BadRequest(result);

            var restore = _procesosSistemas.DotnetRestore(outputRoot);
            result.RestoreOk = restore.Ok;
            result.RestoreOutput = restore.Output;
            result.RestoreError = restore.Error;

            return Ok(result);
        }
    }
}
