using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Tests.Infra;

namespace Backend.Tests.Integracion
{
    [Collection(ColeccionFabrica.Nombre)]
    public class AutenticacionTests
    {
        private readonly EntornoPruebas _e;
        public AutenticacionTests(EntornoPruebas entorno) => _e = entorno;

        [Fact]
        public async Task Login_correcto_devuelve_token()
        {
            var r = await _e.Anonimo().PostAsJsonAsync("/api/v1/auth/login", new { usuario = "admin", password = _e.AdminPassword });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var json = await r.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
        }

        [Fact]
        public async Task No_existen_credenciales_por_defecto_admin_admin()
        {
            Assert.Null(await _e.LoginAsync("admin", "admin"));
        }

        [Fact]
        public async Task Cinco_intentos_fallidos_bloquean_el_usuario_aunque_despues_ponga_la_contrasena_correcta()
        {
            var usuario = await CrearUsuarioAsync();
            for (var i = 0; i < 5; i++)
            {
                var fallo = await _e.Anonimo().PostAsJsonAsync("/api/v1/auth/login", new { usuario = usuario.Username, password = $"mala-{i}" });
                Assert.Equal(HttpStatusCode.Unauthorized, fallo.StatusCode);
            }

            var r = await _e.Anonimo().PostAsJsonAsync("/api/v1/auth/login", new { usuario = usuario.Username, password = usuario.Password });
            Assert.Equal(HttpStatusCode.TooManyRequests, r.StatusCode);
        }

        [Fact]
        public async Task Un_usuario_desactivado_pierde_el_acceso_con_su_token_vigente()
        {
            var usuario = await CrearUsuarioAsync();
            var cliente = await _e.LoginAsync(usuario.Username, usuario.Password);
            Assert.NotNull(cliente);
            Assert.Equal(HttpStatusCode.OK, (await cliente!.GetAsync("/api/v1/menu/tree")).StatusCode);

            var id = await _e.EscalarAsync("SELECT Id FROM dbo.Usuarios WHERE Username = @u", ("@u", usuario.Username));
            await Constructores.EsperarOk(await _e.Admin.PutAsync($"/api/v1/usuarios/{id}/estado?activo=false", null), "desactivar");

            Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/v1/menu/tree")).StatusCode);
        }

        [Fact]
        public async Task El_registro_publico_esta_cerrado_por_defecto()
        {
            var opciones = await _e.Anonimo().GetFromJsonAsync<JsonElement>("/api/v1/auth/opciones");
            Assert.False(opciones.GetProperty("registroPublico").GetBoolean());

            var username = Constructores.Unico("reg");
            var r = await _e.Anonimo().PostAsJsonAsync("/api/v1/auth/registrar", new
            {
                username, email = $"{username}@test.local", password = "Clave-Test-123", nombre = "x", apellido = "y"
            });
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
            Assert.Null(await _e.EscalarAsync("SELECT Id FROM dbo.Usuarios WHERE Username = @u", ("@u", username)));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("abcdefgh")]
        [InlineData("12345678")]
        public async Task Se_rechazan_contrasenas_debiles(string password)
        {
            var rolId = await _e.EscalarAsync("SELECT Id FROM dbo.Roles WHERE Nombre = 'Admin'");
            var username = Constructores.Unico("deb");
            var r = await _e.Admin.PostAsJsonAsync("/api/v1/usuarios", new
            {
                username, email = $"{username}@test.local", password, nombre = "x", apellido = "y", rolId
            });
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }

        private async Task<(string Username, string Password)> CrearUsuarioAsync()
        {
            var rolId = await _e.EscalarAsync("SELECT Id FROM dbo.Roles WHERE Nombre = 'Admin'");
            var username = Constructores.Unico("aut");
            const string password = "Clave-Test-123";
            await Constructores.EsperarOk(await _e.Admin.PostAsJsonAsync("/api/v1/usuarios", new
            {
                username, email = $"{username}@test.local", password, nombre = "Test", apellido = "Auth", rolId
            }), "crear usuario");
            return (username, password);
        }
    }
}
