using Backend.Comun.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Modulos.Sistemas.GeneradorFrontend.Modelos;

namespace Backend.Modulos.Sistemas.GeneradorFrontend
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

        [Authorize(Policy = Politicas.Admin)]
        [HttpPut(Routes.v1.Frontend.GuardarConfig)]
        public IActionResult Guardar(int systemId, [FromBody] FrontendConfigRequest request)
        {
            FrontendConfigGestor.GuardarPorSistema(systemId, request);
            return Ok(new { ok = true });
        }
    }
}
