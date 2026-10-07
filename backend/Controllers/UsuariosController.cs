using Backend.Models.Usuarios;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
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
            UsuariosGestor.Crear(request);
            return Ok();
        }

        [HttpPut(Routes.v1.Usuarios.Editar)]
        public IActionResult Editar(int id, [FromBody] UsuarioUpdateRequest request)
        {
            var ok = UsuariosGestor.Editar(id, request);
            if (!ok)
                return NotFound();

            return Ok();
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
