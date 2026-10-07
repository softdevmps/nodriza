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
        [HttpGet(Routes.v1.Menu.Obtener)]
        public IActionResult ObtenerMenu()
        {
            var usuario = UsuarioToken();

            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var menu = MenuGestor.ObtenerMenuPorUsuario(usuario.UsuarioId);

            return Ok(menu);
        }
        
        [HttpGet(Routes.v1.Menu.Tree)]
        public IActionResult ObtenerMenuTree()
        {
            var usuario = UsuarioToken();

            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var menu = MenuGestor.ObtenerMenuTreePorUsuario(usuario.UsuarioId);

            return Ok(menu);
        }

        [HttpGet(Routes.v1.Menu.SidebarTree)]
        public IActionResult ObtenerSidebarMenuTree()
        {
            var usuario = UsuarioToken();

            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var menu = MenuGestor.ObtenerSidebarTreePorUsuario(usuario.UsuarioId);

            return Ok(menu);
        }

    }
}
