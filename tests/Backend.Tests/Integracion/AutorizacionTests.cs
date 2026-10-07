using System.Net;
using System.Net.Http.Json;
using System.Text;
using Backend.Tests.Infra;

namespace Backend.Tests.Integracion
{
    [Collection(ColeccionFabrica.Nombre)]
    public class AutorizacionTests
    {
        private readonly EntornoPruebas _e;
        public AutorizacionTests(EntornoPruebas entorno) => _e = entorno;

        public static IEnumerable<object[]> RutasDeAdministracion() => new[]
        {
            new object[] { "GET", "/api/v1/usuarios" },
            new object[] { "POST", "/api/v1/usuarios" },
            new object[] { "PUT", "/api/v1/usuarios/1" },
            new object[] { "GET", "/api/v1/roles" },
            new object[] { "PUT", "/api/v1/roles/1/menus" },
            new object[] { "POST", "/api/v1/menu" },
            new object[] { "POST", "/api/v1/sistemas" },
            new object[] { "DELETE", "/api/v1/sistemas/999999" },
            new object[] { "POST", "/api/v1/sistemas/999999/publicar" },
            new object[] { "POST", "/api/v1/sistemas/999999/sql/execute" },
            new object[] { "POST", "/api/v1/sistemas/999999/generar-backend" },
            new object[] { "POST", "/api/v1/sistemas/999999/export" },
            new object[] { "GET", "/api/v1/sistemas/999999/backend-config" },
            new object[] { "POST", "/api/v1/dev/restart" }
        };

        [Theory]
        [MemberData(nameof(RutasDeAdministracion))]
        public async Task Sin_token_responde_401(string metodo, string ruta)
        {
            var r = await Enviar(_e.Anonimo(), metodo, ruta);
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }

        [Theory]
        [MemberData(nameof(RutasDeAdministracion))]
        public async Task Un_usuario_sin_rol_admin_recibe_403(string metodo, string ruta)
        {
            var comun = await Constructores.UsuarioComunAsync(_e);
            var r = await Enviar(comun, metodo, ruta);
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        }

        [Fact]
        public async Task Un_usuario_comun_no_puede_asignarse_el_rol_admin()
        {
            var comun = await Constructores.UsuarioComunAsync(_e);
            var adminRolId = await _e.EscalarAsync("SELECT Id FROM dbo.Roles WHERE Nombre = 'Admin'");
            var adminsAntes = await ContarAdminsAsync();

            var idPropio = await _e.EscalarAsync("SELECT MAX(Id) FROM dbo.Usuarios");
            var r = await comun.PutAsJsonAsync($"/api/v1/usuarios/{idPropio}", new
            {
                username = "x_escalada", email = "x@test.local", nombre = "x", apellido = "y", rolId = adminRolId
            });

            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
            Assert.Equal(adminsAntes, await ContarAdminsAsync());
        }

        private async Task<int> ContarAdminsAsync() => Convert.ToInt32(await _e.EscalarAsync(
            "SELECT COUNT(*) FROM dbo.Usuarios u JOIN dbo.Roles r ON r.Id = u.RolId WHERE r.Nombre = 'Admin'"));

        /// <summary>Body inválido a propósito: si la autorización dejara pasar, no se crea nada.</summary>
        private static Task<HttpResponseMessage> Enviar(HttpClient cliente, string metodo, string ruta)
        {
            var mensaje = new HttpRequestMessage(new HttpMethod(metodo), ruta);
            if (metodo is "POST" or "PUT")
                mensaje.Content = new StringContent("\"body-invalido\"", Encoding.UTF8, "application/json");
            return cliente.SendAsync(mensaje);
        }
    }
}
