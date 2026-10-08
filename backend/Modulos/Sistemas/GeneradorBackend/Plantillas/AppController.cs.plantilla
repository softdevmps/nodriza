using Backend.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    public class AppController : ControllerBase
    {
        protected UsuarioToken UsuarioToken()
        {
            var usuarioId = User.FindFirst("usuarioId")?.Value;
            var usuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return new UsuarioToken
            {
                UsuarioId = usuarioId != null ? int.Parse(usuarioId) : 0,
                Usuario = usuario
            };
        }
    }
}
