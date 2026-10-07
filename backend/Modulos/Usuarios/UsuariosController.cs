using Backend.Comun.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Usuarios.Modelos;

namespace Backend.Modulos.Usuarios
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
    public class UsuariosController : AppController
    {
        [HttpGet(Routes.v1.Usuarios.Obtener)]
        public IActionResult Obtener()
        {
            var usuarios = UsuariosGestor.ObtenerTodos();
            return Ok(usuarios);
        }

        [HttpGet(Routes.v1.Usuarios.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var usuario = UsuariosGestor.ObtenerPorId(id);

            if (usuario == null)
                return NotFound();

            return Ok(usuario);
        }

        [HttpPost(Routes.v1.Usuarios.Crear)]
        public IActionResult Crear([FromBody] UsuarioCreateRequest request)
        {
            var (ok, error) = UsuariosGestor.Crear(request);
            return ok ? Ok() : BadRequest(new { message = error });
        }

        [HttpPut(Routes.v1.Usuarios.Editar)]
        public IActionResult Editar(int id, [FromBody] UsuarioUpdateRequest request)
        {
            var result = UsuariosGestor.Editar(id, request);
            if (result.NotFound)
                return NotFound();

            return result.Ok ? Ok() : BadRequest(new { message = result.Error });
        }

        [HttpPut(Routes.v1.Usuarios.Estado)]
        public IActionResult CambiarEstado(int id, [FromQuery] bool activo)
        {
            var ok = UsuariosGestor.CambiarEstado(id, activo);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
