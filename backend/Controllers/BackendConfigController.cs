using Backend.Models.Sistemas;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class BackendConfigController : AppController
    {
        [HttpGet(Routes.v1.Backend.ObtenerConfig)]
        public IActionResult Obtener(int systemId)
        {
            var config = BackendConfigGestor.ObtenerPorSistema(systemId);
            return Ok(config);
        }

        [HttpPut(Routes.v1.Backend.GuardarConfig)]
        public IActionResult Guardar(int systemId, [FromBody] BackendConfigRequest request)
        {
            BackendConfigGestor.GuardarPorSistema(systemId, request);
            return Ok();
        }
    }
}
