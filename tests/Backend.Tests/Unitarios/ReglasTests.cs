using Backend.Comun;
using Backend.Comun.Seguridad;

namespace Backend.Tests.Unitarios
{
    /// <summary>Reglas puras (sin base de datos): corren en milisegundos.</summary>
    public class ReglasTests
    {
        [Theory]
        [InlineData("Clientes", true)]
        [InlineData("_interno", true)]
        [InlineData("Año2026", true)]
        [InlineData("Columna_1", true)]
        [InlineData("1Empieza", false)]
        [InlineData("con espacio", false)]
        [InlineData("x]; DROP TABLE y;--", false)]
        [InlineData("T\"+codigo+\"", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Identificadores_sql(string? nombre, bool valido) =>
            Assert.Equal(valido, NombresSql.EsIdentificadorValido(nombre));

        [Theory]
        [InlineData("api/v1", true)]
        [InlineData("productos", true)]
        [InlineData("mi-ruta/sub_ruta", true)]
        [InlineData("ruta\";codigo", false)]
        [InlineData("con espacio", false)]
        public void Rutas_de_api(string ruta, bool valida) =>
            Assert.Equal(valida, NombresSql.EsRutaValida(ruta));

        [Theory]
        [InlineData("Clave-123", true)]
        [InlineData("abc12345", true)]
        [InlineData("abcdefgh", false)]
        [InlineData("12345678", false)]
        [InlineData("Ab1", false)]
        [InlineData("", false)]
        public void Politica_de_contrasenas(string password, bool valida) =>
            Assert.Equal(valida, PoliticaContrasenas.Validar(password) == null);

        [Fact]
        public void El_bloqueo_se_activa_al_quinto_fallo_y_se_limpia_con_un_exito()
        {
            var usuario = $"bloqueo_{Guid.NewGuid():N}";
            for (var i = 0; i < BloqueoLogin.MaxIntentos - 1; i++)
                BloqueoLogin.RegistrarFallo(usuario);
            Assert.Null(BloqueoLogin.MinutosBloqueado(usuario));

            BloqueoLogin.RegistrarFallo(usuario);
            Assert.NotNull(BloqueoLogin.MinutosBloqueado(usuario));

            BloqueoLogin.RegistrarExito(usuario);
            Assert.Null(BloqueoLogin.MinutosBloqueado(usuario));
        }

        [Fact]
        public void Los_puertos_de_sistemas_no_chocan_con_la_fabrica_ni_con_vite()
        {
            Assert.Equal(PuertosSistemas.BaseBackend + 1, PuertosSistemas.Backend(1));
            Assert.NotInRange(PuertosSistemas.Frontend(1), 5173, 5199);
            Assert.NotEqual(5032, PuertosSistemas.Backend(1));
        }
    }
}
