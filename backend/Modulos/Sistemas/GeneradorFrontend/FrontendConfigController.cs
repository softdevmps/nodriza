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
        private readonly FrontendConfigGestor _frontendConfigGestor;

        public FrontendConfigController(FrontendConfigGestor frontendConfigGestor)
        {
            _frontendConfigGestor = frontendConfigGestor;
        }

        [HttpGet(Routes.v1.Frontend.ObtenerConfig)]
        public IActionResult Obtener(int systemId)
        {
            var config = _frontendConfigGestor.ObtenerPorSistema(systemId);
            return Ok(config);
        }

        [Authorize(Policy = Politicas.Admin)]
        [HttpPut(Routes.v1.Frontend.GuardarConfig)]
        public IActionResult Guardar(int systemId, [FromBody] FrontendConfigRequest request)
        {
            _frontendConfigGestor.GuardarPorSistema(systemId, request);
            return Ok(new { ok = true });
        }
    }
}
