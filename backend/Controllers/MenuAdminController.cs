using Backend.Models.Menu;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class MenuAdminController : AppController
    {
        [HttpPost(Routes.v1.Menu.Crear)]
        public IActionResult Crear([FromBody] MenuRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            MenuGestor.Crear(request);
            return Ok();
        }

        [HttpPut(Routes.v1.Menu.Editar)]
        public IActionResult Editar(int id, [FromBody] MenuRequest request)
        {
            var ok = MenuGestor.Editar(id, request);
            return ok ? Ok() : NotFound();
        }

        [HttpPut(Routes.v1.Menu.Desactivar)]
        public IActionResult Desactivar(int id)
        {
            var ok = MenuGestor.Desactivar(id);
            return ok ? Ok() : NotFound();
        }
    }
}
