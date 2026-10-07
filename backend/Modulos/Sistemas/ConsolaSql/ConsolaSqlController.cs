using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.Seguridad;
using Backend.Modulos.Sistemas.ConsolaSql.Modelos;

namespace Backend.Modulos.Sistemas.ConsolaSql
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
    public class ConsolaSqlController : AppController
    {
        private readonly IWebHostEnvironment _env;

        public ConsolaSqlController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost(Routes.v1.Sistemas.EjecutarSql)]
        public IActionResult EjecutarSql(int id, [FromBody] SqlScriptExecuteRequest request)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(new { message = "Script SQL invalido." });

            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var (ok, error, resultado) = ConsolaSqlGestor.Ejecutar(id, sistema.Slug, request.Script, request.ImportMetadata);
            return ok ? Ok(resultado) : BadRequest(new { message = error });
        }
    }
}
