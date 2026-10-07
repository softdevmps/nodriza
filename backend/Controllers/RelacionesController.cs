using Backend.Models.Sistemas;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class RelacionesController : AppController
    {
        [HttpGet(Routes.v1.Relaciones.Obtener)]
        public IActionResult Obtener(int systemId)
        {
            var relaciones = RelacionesGestor.ObtenerPorSistema(systemId);
            return Ok(relaciones);
        }

        [HttpPost(Routes.v1.Relaciones.Crear)]
        public IActionResult Crear(int systemId, [FromBody] RelacionCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = RelacionesGestor.Crear(systemId, request);
            if (id == null)
                return BadRequest("Relacion invalida.");

            return Ok(new { id });
        }

        [HttpPut(Routes.v1.Relaciones.Editar)]
        public IActionResult Editar(int systemId, int id, [FromBody] RelacionUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = RelacionesGestor.Editar(systemId, id, request);
            return ok ? Ok() : NotFound();
        }
    }
}
