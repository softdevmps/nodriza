using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.Seguridad;

namespace Backend.Modulos.Menu
{
    [ApiController]
    [Authorize]
    public class MenuController : AppController
    {
        private readonly MenuGestor _menuGestor;

        public MenuController(MenuGestor menuGestor)
        {
            _menuGestor = menuGestor;
        }

        [HttpGet(Routes.v1.Menu.Obtener)]
        public IActionResult ObtenerMenu()
        {
            var usuario = UsuarioToken();

            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var menu = _menuGestor.ObtenerMenuPorUsuario(usuario.UsuarioId);

            return Ok(menu);
        }
        
        [HttpGet(Routes.v1.Menu.Tree)]
        public IActionResult ObtenerMenuTree()
        {
            var usuario = UsuarioToken();

            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var menu = _menuGestor.ObtenerMenuTreePorUsuario(usuario.UsuarioId);

            return Ok(menu);
        }

        [HttpGet(Routes.v1.Menu.SidebarTree)]
        public IActionResult ObtenerSidebarMenuTree()
        {
            var usuario = UsuarioToken();

            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var menu = _menuGestor.ObtenerSidebarTreePorUsuario(usuario.UsuarioId);

            return Ok(menu);
        }

    }
}
