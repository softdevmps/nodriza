using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.Seguridad;
using Backend.Modulos.Sistemas.GeneradorBackend;
using Backend.Modulos.Sistemas.Herramientas;
using Backend.Modulos.Sistemas.Modelos;
using Backend.Modulos.Sistemas.Publicacion;

namespace Backend.Modulos.Sistemas
{
    /// <summary>
    /// Alta, edición, borrado (archivado) y publicación de sistemas. El resto vive en su submódulo:
    /// ConsolaSql/, Exportacion/, GeneradorBackend/, GeneradorFrontend/ y Herramientas/.
    /// </summary>
    [ApiController]
    [Authorize]
    public class SistemasController : AppController
    {
        private readonly IDbContextFactory<SystemBaseContext> _contextos;
        private readonly ProcesosSistemas _procesosSistemas;
        private readonly SistemasGestor _sistemasGestor;
        private readonly SistemasPublicador _sistemasPublicador;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SistemasController> _logger;

        public SistemasController(IWebHostEnvironment env, ILogger<SistemasController> logger, IDbContextFactory<SystemBaseContext> contextos, ProcesosSistemas procesosSistemas, SistemasGestor sistemasGestor, SistemasPublicador sistemasPublicador)
        {
            _contextos = contextos;
            _procesosSistemas = procesosSistemas;
            _sistemasGestor = sistemasGestor;
            _sistemasPublicador = sistemasPublicador;
            _env = env;
            _logger = logger;
        }

        [HttpGet(Routes.v1.Sistemas.Obtener)]
        public IActionResult Obtener()
        {
            var sistemas = _sistemasGestor.ObtenerTodos();
            return Ok(sistemas);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var sistema = _sistemasGestor.ObtenerPorId(id);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorSlug)]
        public IActionResult ObtenerPorSlug(string slug)
        {
            var sistema = _sistemasGestor.ObtenerPorSlug(slug);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPost(Routes.v1.Sistemas.Crear)]
        public IActionResult Crear([FromBody] SistemaCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = _sistemasGestor.Crear(request);
            if (id == null)
                return Conflict("Slug invalido o ya existe.");

            return Ok(new { id });
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPut(Routes.v1.Sistemas.Editar)]
        public IActionResult Editar(int id, [FromBody] SistemaUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = _sistemasGestor.Editar(id, request);
            return ok ? Ok() : NotFound();
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpDelete(Routes.v1.Sistemas.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var sistema = _sistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            // Los procesos generados tienen archivos abiertos en la carpeta que se va a archivar.
            _procesosSistemas.Detener(Componente.Backend, id);
            _procesosSistemas.Detener(Componente.Frontend, id);

            var result = _sistemasGestor.Eliminar(id);
            if (result.NotFound)
                return NotFound();

            if (!result.Ok)
            {
                _logger.LogError("No se pudo eliminar el sistema {Id}: {Error}", id, result.Error);
                return BadRequest(new { message = "No se pudo eliminar el sistema. No se aplicó ningún cambio." });
            }

            var carpetaArchivada = ArchivarCarpetaSistema(sistema.Slug);

            // Sin sistema no hay credenciales: se borran su login y usuarios SQL (los datos quedan archivados).
            try
            {
                using var context = _contextos.CreateDbContext();
                CredencialesSistema.Eliminar(context, sistema.Slug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron borrar las credenciales SQL del sistema {Slug}", sistema.Slug);
            }
            return Ok(new { schemaArchivado = result.SchemaArchivado, carpetaArchivada });
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPost(Routes.v1.Sistemas.Publicar)]
        public IActionResult Publicar(int id)
        {
            var result = _sistemasPublicador.Publicar(id);
            return result.Ok ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Mueve systems/&lt;slug&gt; a systems/_eliminados/&lt;slug&gt;_&lt;fecha&gt;: el código generado (y lo
        /// que se haya escrito a mano) no se pierde y el slug queda libre. Devuelve la ruta relativa o null.
        /// </summary>
        private string? ArchivarCarpetaSistema(string slug)
        {
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var systemsRoot = Path.Combine(repoRoot, "systems");
            var origen = Path.Combine(systemsRoot, slug);
            if (!Directory.Exists(origen))
                return null;

            var nombre = $"{slug}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var destino = Path.Combine(systemsRoot, "_eliminados", nombre);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
                Directory.Move(origen, destino);
                return Path.Combine("systems", "_eliminados", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo archivar la carpeta del sistema {Slug}", slug);
                return null;
            }
        }
    }
}
