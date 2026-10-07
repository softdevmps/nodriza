using System.Net;
using System.Net.Http.Json;
using Backend.Tests.Infra;
using static Backend.Tests.Infra.Constructores;

namespace Backend.Tests.Integracion
{
    [Collection(ColeccionFabrica.Nombre)]
    public class DisenoTests
    {
        private readonly EntornoPruebas _e;
        public DisenoTests(EntornoPruebas entorno) => _e = entorno;

        [Theory]
        [InlineData("x]; DROP TABLE dbo.Usuarios;--")]
        [InlineData("T\" + System.Environment.Exit(1) + \"")]
        [InlineData("con espacio")]
        public async Task Un_nombre_de_tabla_inseguro_se_rechaza_al_crearlo(string tabla)
        {
            var s = await CrearSistemaAsync(_e.Admin);
            var r = await _e.Admin.PostAsJsonAsync($"/api/v1/sistemas/{s.Id}/entidades", new { name = "Mala", tableName = tabla });
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }

        [Theory]
        [InlineData("Ñandú")]
        [InlineData("1empieza")]
        [InlineData("../escape")]
        [InlineData("guion-medio")]
        public async Task Un_slug_que_no_es_ascii_seguro_se_rechaza(string slug)
        {
            var r = await _e.Admin.PostAsJsonAsync("/api/v1/sistemas", new { slug, name = "x", @namespace = "x" });
            Assert.NotEqual(HttpStatusCode.OK, r.StatusCode);
        }

        [Theory]
        [InlineData("ManyToMany")]
        [InlineData("OneToMany")]
        public async Task Las_relaciones_no_soportadas_se_rechazan(string tipo)
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("A", Pk()), new Entidad("B", Pk(), Entero("AId")));
            var r = await _e.Admin.PostAsJsonAsync($"/api/v1/sistemas/{s.Id}/relaciones", new
            {
                sourceEntityId = s.Entidades["B"], targetEntityId = s.Entidades["A"], relationType = tipo, foreignKey = "AId"
            });
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }

        [Fact]
        public async Task Una_relacion_con_FK_inexistente_se_rechaza()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("A", Pk()), new Entidad("B", Pk()));
            var r = await _e.Admin.PostAsJsonAsync($"/api/v1/sistemas/{s.Id}/relaciones", new
            {
                sourceEntityId = s.Entidades["B"], targetEntityId = s.Entidades["A"], relationType = "ManyToOne", foreignKey = "NoExiste"
            });
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }
    }
}
