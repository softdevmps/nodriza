using Backend.Comun.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Modulos.Sistemas.GeneradorBackend.Modelos;

namespace Backend.Modulos.Sistemas.GeneradorBackend
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
    public class BackendConfigController : AppController
    {
        private readonly BackendConfigGestor _backendConfigGestor;

        public BackendConfigController(BackendConfigGestor backendConfigGestor)
        {
            _backendConfigGestor = backendConfigGestor;
        }

        [HttpGet(Routes.v1.Backend.ObtenerConfig)]
        public IActionResult Obtener(int systemId)
        {
            var config = _backendConfigGestor.ObtenerPorSistema(systemId);
            return Ok(config);
        }

        [HttpPut(Routes.v1.Backend.GuardarConfig)]
        public IActionResult Guardar(int systemId, [FromBody] BackendConfigRequest request)
        {
            _backendConfigGestor.GuardarPorSistema(systemId, request);
            return Ok();
        }
    }
}
