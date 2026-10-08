using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.IO;

namespace Backend.Controllers
{
    [ApiController]
    public class DevToolsController : AppController
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IWebHostEnvironment _env;

        public DevToolsController(IHostApplicationLifetime lifetime, IWebHostEnvironment env)
        {
            _lifetime = lifetime;
            _env = env;
        }

        [HttpPost(Routes.v1.DevTools.Restart)]
        [AllowAnonymous]
        public IActionResult Restart()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var remote = HttpContext.Connection.RemoteIpAddress;
            if (remote == null || !IPAddress.IsLoopback(remote))
                return Forbid();

            TouchRestartSignal();
            _ = Task.Run(() => _lifetime.StopApplication());

            return Ok(new { message = "Reiniciando backend..." });
        }

        [HttpGet(Routes.v1.DevTools.Ping)]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            return Ok(new { status = "ok" });
        }

        private void TouchRestartSignal()
        {
            try
            {
                var path = Path.Combine(_env.ContentRootPath, ".restart");
                System.IO.File.WriteAllText(path, DateTime.UtcNow.ToString("O"));
            }
            catch
            {
                // no-op
            }
        }
    }
}
