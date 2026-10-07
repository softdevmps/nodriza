using System.Text.Json;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class DatosController : AppController
    {
        [HttpGet(Routes.v1.Datos.Obtener)]
        public IActionResult Obtener(int systemId, int entityId, [FromQuery] int? take)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var result = DatosGestor.Listar(systemId, entityId, take, usuario.UsuarioId);
            return result.Ok ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPost(Routes.v1.Datos.Crear)]
        public IActionResult Crear(int systemId, int entityId, [FromBody] Dictionary<string, JsonElement> data)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var result = DatosGestor.Crear(systemId, entityId, data, usuario.UsuarioId);
            return result.Ok ? Ok() : BadRequest(result.Error);
        }

        [HttpPut(Routes.v1.Datos.Editar)]
        public IActionResult Editar(int systemId, int entityId, string id, [FromBody] Dictionary<string, JsonElement> data)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var result = DatosGestor.Editar(systemId, entityId, id, data, usuario.UsuarioId);
            return result.Ok ? Ok() : BadRequest(result.Error);
        }

        [HttpDelete(Routes.v1.Datos.Eliminar)]
        public IActionResult Eliminar(int systemId, int entityId, string id)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var result = DatosGestor.Eliminar(systemId, entityId, id, usuario.UsuarioId);
            return result.Ok ? Ok() : BadRequest(result.Error);
        }
    }
}
