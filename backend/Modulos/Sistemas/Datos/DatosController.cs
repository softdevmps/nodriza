using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.Seguridad;

namespace Backend.Modulos.Sistemas.Datos
{
    [ApiController]
    [Authorize]
    public class DatosController : AppController
    {
        private readonly DatosGestor _datosGestor;

        public DatosController(DatosGestor datosGestor)
        {
            _datosGestor = datosGestor;
        }

        [HttpGet(Routes.v1.Datos.Obtener)]
        public IActionResult Obtener(int systemId, int entityId, [FromQuery] int? take, [FromQuery] int? skip)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var result = _datosGestor.Listar(systemId, entityId, take, skip, usuario.UsuarioId);
            return result.Ok ? Ok(result.Data) : Fallo(result.Error, result.SinPermiso);
        }

        [HttpPost(Routes.v1.Datos.Crear)]
        public IActionResult Crear(int systemId, int entityId, [FromBody] Dictionary<string, JsonElement> data)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var result = _datosGestor.Crear(systemId, entityId, data, usuario.UsuarioId);
            return result.Ok ? Ok() : Fallo(result.Error, result.SinPermiso);
        }

        [HttpPut(Routes.v1.Datos.Editar)]
        public IActionResult Editar(int systemId, int entityId, string id, [FromBody] Dictionary<string, JsonElement> data)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var result = _datosGestor.Editar(systemId, entityId, id, data, usuario.UsuarioId);
            return result.Ok ? Ok() : Fallo(result.Error, result.SinPermiso);
        }

        [HttpDelete(Routes.v1.Datos.Eliminar)]
        public IActionResult Eliminar(int systemId, int entityId, string id)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var result = _datosGestor.Eliminar(systemId, entityId, id, usuario.UsuarioId);
            return result.Ok ? Ok() : Fallo(result.Error, result.SinPermiso);
        }
    
        // Sin permiso sobre la entidad → 403; cualquier otro problema de datos → 400.
        private IActionResult Fallo(string? error, bool sinPermiso) =>
            sinPermiso ? StatusCode(StatusCodes.Status403Forbidden, error) : BadRequest(error);
    }
}
