using Backend.Comun.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Modulos.Sistemas.Relaciones.Modelos;

namespace Backend.Modulos.Sistemas.Relaciones
{
    [ApiController]
    [Authorize]
    public class RelacionesController : AppController
    {
        private readonly RelacionesGestor _relacionesGestor;

        public RelacionesController(RelacionesGestor relacionesGestor)
        {
            _relacionesGestor = relacionesGestor;
        }

        [HttpGet(Routes.v1.Relaciones.Obtener)]
        public IActionResult Obtener(int systemId)
        {
            var relaciones = _relacionesGestor.ObtenerPorSistema(systemId);
            return Ok(relaciones);
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPost(Routes.v1.Relaciones.Crear)]
        public IActionResult Crear(int systemId, [FromBody] RelacionCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var (id, error) = _relacionesGestor.Crear(systemId, request);
            if (id == null)
                return BadRequest(new { message = error });

            return Ok(new { id });
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPut(Routes.v1.Relaciones.Editar)]
        public IActionResult Editar(int systemId, int id, [FromBody] RelacionUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var result = _relacionesGestor.Editar(systemId, id, request);
            if (result.NotFound)
                return NotFound();
            return result.Ok ? Ok() : BadRequest(new { message = result.Error });
        }
    }
}
