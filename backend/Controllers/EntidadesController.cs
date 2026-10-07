using Backend.Models.Sistemas;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class EntidadesController : AppController
    {
        [HttpGet(Routes.v1.Entidades.Obtener)]
        public IActionResult Obtener(int systemId)
        {
            var entidades = EntidadesGestor.ObtenerPorSistema(systemId);
            return Ok(entidades);
        }

        [HttpGet(Routes.v1.Entidades.ObtenerRuntime)]
        public IActionResult ObtenerRuntime(int systemId)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var entidades = EntidadesGestor.ObtenerParaRuntime(systemId, usuario.UsuarioId);
            return Ok(entidades);
        }

        [HttpGet(Routes.v1.Entidades.ObtenerPorId)]
        public IActionResult ObtenerPorId(int systemId, int id)
        {
            var entidad = EntidadesGestor.ObtenerPorId(systemId, id);
            return entidad == null ? NotFound() : Ok(entidad);
        }

        [HttpGet(Routes.v1.Entidades.ObtenerPorNombre)]
        public IActionResult ObtenerPorNombre(int systemId, string name)
        {
            var entidad = EntidadesGestor.ObtenerPorNombre(systemId, name);
            return entidad == null ? NotFound() : Ok(entidad);
        }

        [HttpPost(Routes.v1.Entidades.Crear)]
        public IActionResult Crear(int systemId, [FromBody] EntidadCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = EntidadesGestor.Crear(systemId, request);
            if (id == null)
                return Conflict("Entidad duplicada o sistema inexistente.");

            return Ok(new { id });
        }

        [HttpPut(Routes.v1.Entidades.Editar)]
        public IActionResult Editar(int systemId, int id, [FromBody] EntidadUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = EntidadesGestor.Editar(systemId, id, request);
            return ok ? Ok() : NotFound();
        }

        [HttpDelete(Routes.v1.Entidades.Eliminar)]
        public IActionResult Eliminar(int systemId, int id, [FromQuery] bool dropTable = false)
        {
            var result = EntidadesGestor.Eliminar(systemId, id, dropTable);
            if (!result.Ok)
            {
                if (result.NotFound)
                    return NotFound();

                return BadRequest(new
                {
                    message = result.Error ?? "No se pudo eliminar la entidad."
                });
            }

            return Ok(new
            {
                message = result.Message,
                runtimeTableDropped = result.RuntimeTableDropped
            });
        }
    }
}
