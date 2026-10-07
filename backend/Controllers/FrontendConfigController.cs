using Backend.Models.Sistemas;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class FrontendConfigController : AppController
    {
        [HttpGet(Routes.v1.Frontend.ObtenerConfig)]
        public IActionResult Obtener(int systemId)
        {
            var config = FrontendConfigGestor.ObtenerPorSistema(systemId);
            return Ok(config);
        }

        [HttpPut(Routes.v1.Frontend.GuardarConfig)]
        public IActionResult Guardar(int systemId, [FromBody] FrontendConfigRequest request)
        {
            FrontendConfigGestor.GuardarPorSistema(systemId, request);
            return Ok(new { ok = true });
        }
    }
}
