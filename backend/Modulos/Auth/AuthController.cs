using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Modulos.Auth.Modelos;

namespace Backend.Modulos.Auth
{
    [ApiController]
    public class AuthController : AppController
    {
        [HttpPost(Routes.v1.Auth.Registrar)]
        public IActionResult Registrar([FromBody] RegistrarRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Datos inválidos");

            var ok = AuthGestor.Registrar(model);

            if (!ok)
                return BadRequest("Usuario o email ya existente");

            return Ok("Usuario creado correctamente");
        }

        [HttpPost(Routes.v1.Auth.Login)]
        public IActionResult Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Datos incorrectos");

            var result = AuthGestor.Login(model);

            if (result == null)
                return Unauthorized("Usuario o contraseña incorrectos");

            return Ok(result);
        }
    }
}
