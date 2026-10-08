using Backend.Comun.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Modulos.Sistemas.Campos.Modelos;

namespace Backend.Modulos.Sistemas.Campos
{
    [ApiController]
    [Authorize]
    public class CamposController : AppController
    {
        private readonly CamposGestor _camposGestor;

        public CamposController(CamposGestor camposGestor)
        {
            _camposGestor = camposGestor;
        }

        [HttpGet(Routes.v1.Campos.Obtener)]
        public IActionResult Obtener(int systemId, int entityId)
        {
            var campos = _camposGestor.ObtenerPorEntidad(systemId, entityId);
            return Ok(campos);
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPost(Routes.v1.Campos.Crear)]
        public IActionResult Crear(int systemId, int entityId, [FromBody] CampoCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = _camposGestor.Crear(systemId, entityId, request);
            if (id == null)
                return Conflict("Campo duplicado, tipo invalido o entidad inexistente.");

            return Ok(new { id });
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPut(Routes.v1.Campos.Editar)]
        public IActionResult Editar(int systemId, int entityId, int id, [FromBody] CampoUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = _camposGestor.Editar(systemId, entityId, id, request);
            return ok ? Ok() : NotFound();
        }
    }
}
