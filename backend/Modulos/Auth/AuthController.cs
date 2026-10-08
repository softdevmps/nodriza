using Backend.Comun.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Modulos.Auth.Modelos;

namespace Backend.Modulos.Auth
{
    [ApiController]
    public class AuthController : AppController
    {
        private readonly AuthGestor _authGestor;

        public AuthController(AuthGestor authGestor)
        {
            _authGestor = authGestor;
        }

        /// <summary>Opciones públicas para la pantalla de login (ej.: si se muestra "Registrarse").</summary>
        [HttpGet(Routes.v1.Auth.Opciones)]
        public IActionResult Opciones() => Ok(new { registroPublico = AppConfig.REGISTRO_PUBLICO });

        [HttpPost(Routes.v1.Auth.Registrar)]
        public IActionResult Registrar([FromBody] RegistrarRequest model)
        {
            if (!AppConfig.REGISTRO_PUBLICO)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "El registro público está deshabilitado. Pedile a un administrador que te cree el usuario." });

            if (!ModelState.IsValid)
                return BadRequest("Datos inválidos");

            var error = _authGestor.Registrar(model);
            if (error != null)
                return BadRequest(error);

            return Ok("Usuario creado correctamente");
        }

        [HttpPost(Routes.v1.Auth.Login)]
        public IActionResult Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Datos incorrectos");

            if (BloqueoLogin.MinutosBloqueado(model.Usuario) is { } minutos)
                return StatusCode(StatusCodes.Status429TooManyRequests, $"Demasiados intentos fallidos. Probá de nuevo en {minutos} minuto(s).");

            var result = _authGestor.Login(model);

            if (result == null)
            {
                BloqueoLogin.RegistrarFallo(model.Usuario);
                return Unauthorized("Usuario o contraseña incorrectos");
            }

            BloqueoLogin.RegistrarExito(model.Usuario);
            return Ok(result);
        }
}
}
