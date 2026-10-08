using Backend.Comun.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Modulos.Menu.Modelos;

namespace Backend.Modulos.Menu
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
    public class MenuAdminController : AppController
    {
        private readonly MenuGestor _menuGestor;

        public MenuAdminController(MenuGestor menuGestor)
        {
            _menuGestor = menuGestor;
        }

        [HttpPost(Routes.v1.Menu.Crear)]
        public IActionResult Crear([FromBody] MenuRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            _menuGestor.Crear(request);
            return Ok();
        }

        [HttpPut(Routes.v1.Menu.Editar)]
        public IActionResult Editar(int id, [FromBody] MenuRequest request)
        {
            var ok = _menuGestor.Editar(id, request);
            return ok ? Ok() : NotFound();
        }

        [HttpPut(Routes.v1.Menu.Desactivar)]
        public IActionResult Desactivar(int id)
        {
            var ok = _menuGestor.Desactivar(id);
            return ok ? Ok() : NotFound();
        }
    }
}
