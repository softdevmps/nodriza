using Backend.Comun.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Roles.Modelos;

namespace Backend.Modulos.Roles
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
    public class RolesController : AppController
    {
        private readonly RolesGestor _rolesGestor;

        public RolesController(RolesGestor rolesGestor)
        {
            _rolesGestor = rolesGestor;
        }

        [HttpGet(Routes.v1.Roles.Obtener)]
        public IActionResult ObtenerRoles()
        {
            var roles = _rolesGestor.ObtenerTodos();
            return Ok(roles);
        }

        [HttpGet(Routes.v1.Roles.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var rol = _rolesGestor.ObtenerPorId(id);

            if (rol == null)
                return NotFound();

            return Ok(rol);
        }


        [HttpPost(Routes.v1.Roles.Crear)]
        public IActionResult Crear([FromBody] RolCreateRequest request)
        {
            _rolesGestor.Crear(request);
            return Ok();
        }

        [HttpPut(Routes.v1.Roles.Editar)]
        public IActionResult Editar(int id, [FromBody] RolUpdateRequest request)
        {
            var ok = _rolesGestor.Editar(id, request);
            if (!ok)
                return NotFound();

            return Ok();
        }

        [HttpPut(Routes.v1.Roles.Estado)]
        public IActionResult CambiarEstado(int id, [FromQuery] bool activo)
        {
            var ok = _rolesGestor.CambiarEstado(id, activo);
            if (!ok)
                return NotFound();

            return Ok();
        }

        [HttpPut(Routes.v1.Roles.AsignarMenus)]
        public IActionResult AsignarMenus(int id, [FromBody] RolMenusRequest request)
        {
            var ok = _rolesGestor.AsignarMenus(id, request.MenusIds);
            if (!ok)
                return NotFound();

            return Ok();
        }

        [HttpGet(Routes.v1.Roles.ObtenerSystemMenus)]
        public IActionResult ObtenerSystemMenus(int id)
        {
            var menus = _rolesGestor.ObtenerSystemMenusPorRol(id);
            if (menus == null)
                return NotFound();

            return Ok(menus);
        }

        [HttpPut(Routes.v1.Roles.AsignarSystemMenus)]
        public IActionResult AsignarSystemMenus(int id, [FromBody] RolSystemMenusRequest request)
        {
            var ok = _rolesGestor.AsignarSystemMenus(id, request.SystemIds);
            if (!ok)
                return NotFound();

            return Ok();
        }

        [HttpGet(Routes.v1.Roles.ObtenerPermisos)]
        public IActionResult ObtenerPermisos(int id, int systemId)
        {
            var permisos = _rolesGestor.ObtenerPermisosPorRol(id, systemId);
            if (permisos == null)
                return NotFound();

            return Ok(permisos);
        }

        [HttpPut(Routes.v1.Roles.AsignarPermisos)]
        public IActionResult AsignarPermisos(int id, int systemId, [FromBody] RolPermissionsRequest request)
        {
            var ok = _rolesGestor.AsignarPermisos(id, systemId, request.PermissionIds);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
