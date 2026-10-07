using Backend.Models.Sistemas;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class CamposController : AppController
    {
        [HttpGet(Routes.v1.Campos.Obtener)]
        public IActionResult Obtener(int systemId, int entityId)
        {
            var campos = CamposGestor.ObtenerPorEntidad(systemId, entityId);
            return Ok(campos);
        }

        [HttpPost(Routes.v1.Campos.Crear)]
        public IActionResult Crear(int systemId, int entityId, [FromBody] CampoCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = CamposGestor.Crear(systemId, entityId, request);
            if (id == null)
                return Conflict("Campo duplicado, tipo invalido o entidad inexistente.");

            return Ok(new { id });
        }

        [HttpPut(Routes.v1.Campos.Editar)]
        public IActionResult Editar(int systemId, int entityId, int id, [FromBody] CampoUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = CamposGestor.Editar(systemId, entityId, id, request);
            return ok ? Ok() : NotFound();
        }
    }
}
