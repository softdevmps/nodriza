using System.Net;
using System.Net.Http.Json;
using Backend.Tests.Infra;
using static Backend.Tests.Infra.Constructores;

namespace Backend.Tests.Integracion
{
    /// <summary>Runtime de datos: que lo que se guarda sea exactamente lo que se envió.</summary>
    [Collection(ColeccionFabrica.Nombre)]
    public class DatosTests : IAsyncLifetime
    {
        private readonly EntornoPruebas _e;
        private Sistema _s = null!;
        public DatosTests(EntornoPruebas entorno) => _e = entorno;

        public async Task InitializeAsync()
        {
            _s = await CrearSistemaAsync(_e.Admin,
                new Entidad("Fichas", Pk(), Texto("Clave", 40, requerido: true), Decimal("Saldo", 18, 2), Fecha("Alta")),
                new Entidad("Clientes", Pk(), Texto("Email", 100, unico: true)));
            await PublicarAsync(_e.Admin, _s);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private Task<object?> Leer(string clave, string expresion) =>
            _e.EscalarAsync($"SELECT {expresion} FROM [{_s.Schema}].[Fichas] WHERE Clave = @c", ("@c", clave));

        [Theory]
        [InlineData("10.50", "10.50")]
        [InlineData("0.5", "0.50")]
        [InlineData("1234567890123456.78", "1234567890123456.78")]
        public async Task Un_decimal_enviado_como_texto_se_guarda_exacto(string enviado, string esperado)
        {
            var clave = Unico("dec");
            var r = await _e.Admin.PostAsJsonAsync(_s.Datos("Fichas"), new { Clave = clave, Saldo = enviado });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal(esperado, await Leer(clave, "CAST(Saldo AS VARCHAR(40))"));
        }

        [Theory]
        [InlineData("10,50")]
        [InlineData("1.000.000")]
        [InlineData("1.234")]
        public async Task Un_decimal_ambiguo_o_fuera_de_escala_se_rechaza_sin_guardar(string enviado)
        {
            var clave = Unico("amb");
            var r = await _e.Admin.PostAsJsonAsync(_s.Datos("Fichas"), new { Clave = clave, Saldo = enviado });
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Null(await Leer(clave, "Id"));
        }

        [Fact]
        public async Task Una_fecha_UTC_se_guarda_sin_correrse_y_sin_perder_precision()
        {
            var utc = Unico("utc");
            await EsperarOk(await _e.Admin.PostAsJsonAsync(_s.Datos("Fichas"), new { Clave = utc, Alta = "2026-10-07T13:45:30Z" }), "alta UTC");
            Assert.Equal("2026-10-07T13:45:30", await Leer(utc, "CONVERT(VARCHAR(19), Alta, 126)"));

            var precisa = Unico("pre");
            await EsperarOk(await _e.Admin.PostAsJsonAsync(_s.Datos("Fichas"), new { Clave = precisa, Alta = "2026-10-07T13:45:30.1234567" }), "alta precisa");
            Assert.Equal("2026-10-07T13:45:30.1234567", await Leer(precisa, "CONVERT(VARCHAR(33), Alta, 126)"));
        }

        [Fact]
        public async Task Un_valor_unico_duplicado_responde_400_y_no_duplica()
        {
            var email = $"{Unico("mail")}@test.local";
            await EsperarOk(await _e.Admin.PostAsJsonAsync(_s.Datos("Clientes"), new { Email = email }), "primer alta");

            var r = await _e.Admin.PostAsJsonAsync(_s.Datos("Clientes"), new { Email = email });

            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal(1, Convert.ToInt32(await _e.EscalarAsync($"SELECT COUNT(*) FROM [{_s.Schema}].[Clientes] WHERE Email = @e", ("@e", email))));
        }

        [Fact]
        public async Task Un_valor_con_SQL_se_guarda_como_texto_literal()
        {
            const string valor = "'); DROP TABLE dbo.Usuarios; --";
            await EsperarOk(await _e.Admin.PostAsJsonAsync(_s.Datos("Fichas"), new { Clave = valor }), "alta");
            Assert.NotNull(await _e.EscalarAsync("SELECT OBJECT_ID('dbo.Usuarios', 'U')"));
            Assert.NotNull(await Leer(valor, "Id"));
        }

        [Fact]
        public async Task Sin_permiso_sobre_la_entidad_responde_403()
        {
            var comun = await UsuarioComunAsync(_e);
            var r = await comun.GetAsync(_s.Datos("Fichas"));
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        }
    }
}
