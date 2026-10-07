using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Tests.Infra;
using static Backend.Tests.Infra.Constructores;

namespace Backend.Tests.Integracion
{
    [Collection(ColeccionFabrica.Nombre)]
    public class PublicacionTests
    {
        private readonly EntornoPruebas _e;
        public PublicacionTests(EntornoPruebas entorno) => _e = entorno;

        [Fact]
        public async Task Primera_publicacion_crea_la_tabla_y_el_admin_puede_cargar_datos()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Clientes", Pk(), Texto("Nombre", 80, requerido: true)));
            await PublicarAsync(_e.Admin, s);

            Assert.NotNull(await _e.EscalarAsync("SELECT OBJECT_ID(@t, 'U')", ("@t", $"{s.Schema}.Clientes")));
            var r = await _e.Admin.PostAsJsonAsync(s.Datos("Clientes"), new { Nombre = "Primero" });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        [Fact]
        public async Task Republicar_no_pierde_datos()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Notas", Pk(), Texto("Texto", 50)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Notas] (Texto) VALUES (N'conservar')");

            await PublicarAsync(_e.Admin, s);
            await PublicarAsync(_e.Admin, s);

            Assert.Equal(1, Convert.ToInt32(await _e.EscalarAsync($"SELECT COUNT(*) FROM [{s.Schema}].[Notas] WHERE Texto = N'conservar'")));
        }

        [Fact]
        public async Task Agrandar_un_texto_se_aplica_y_achicarlo_con_datos_largos_se_rechaza_sin_truncar()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Items", Pk(), Texto("Nombre", 50)));
            await PublicarAsync(_e.Admin, s);
            var valor = new string('x', 40);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Items] (Nombre) VALUES (@v)", ("@v", valor));
            var campoId = await CampoIdAsync(s, "Items", "Nombre");

            await EditarLargoAsync(s, "Items", campoId, 200);
            await PublicarAsync(_e.Admin, s);
            Assert.Equal(200, await LargoColumnaAsync(s.Schema, "Items", "Nombre"));

            await EditarLargoAsync(s, "Items", campoId, 10);
            var r = await _e.Admin.PostAsync($"/api/v1/sistemas/{s.Id}/publicar", null);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal(200, await LargoColumnaAsync(s.Schema, "Items", "Nombre"));
            Assert.Equal(valor, await _e.EscalarAsync($"SELECT Nombre FROM [{s.Schema}].[Items]"));
        }

        [Fact]
        public async Task Un_campo_obligatorio_nuevo_en_tabla_con_datos_necesita_valor_por_defecto()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Stock", Pk(), Texto("Codigo", 20)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Stock] (Codigo) VALUES ('A')");

            var r = await _e.Admin.PostAsJsonAsync($"/api/v1/sistemas/{s.Id}/entidades/{s.Entidades["Stock"]}/campos",
                new { name = "Minimo", columnName = "Minimo", dataType = "int", required = true, sortOrder = 3 });
            await EsperarOk(r, "crear campo");
            var campoId = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var sinDefault = await _e.Admin.PostAsync($"/api/v1/sistemas/{s.Id}/publicar", null);
            Assert.Equal(HttpStatusCode.BadRequest, sinDefault.StatusCode);
            Assert.Contains("Minimo", await sinDefault.Content.ReadAsStringAsync());

            await EsperarOk(await _e.Admin.PutAsJsonAsync($"/api/v1/sistemas/{s.Id}/entidades/{s.Entidades["Stock"]}/campos/{campoId}",
                new { name = "Minimo", columnName = "Minimo", dataType = "int", required = true, defaultValue = "5", sortOrder = 3 }), "editar campo");
            await PublicarAsync(_e.Admin, s);
            Assert.Equal(5, Convert.ToInt32(await _e.EscalarAsync($"SELECT Minimo FROM [{s.Schema}].[Stock]")));
        }

        [Fact]
        public async Task Si_la_publicacion_falla_no_queda_nada_a_medias()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("A", Pk()), new Entidad("B", Pk()));
            // Un objeto ajeno con el nombre de la tabla B hace fallar el CREATE TABLE después de crear A
            await _e.EjecutarAsync($"CREATE SCHEMA [{s.Schema}]");
            await _e.EjecutarAsync($"CREATE VIEW [{s.Schema}].[B] AS SELECT 1 AS x");

            var r = await _e.Admin.PostAsync($"/api/v1/sistemas/{s.Id}/publicar", null);

            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.DoesNotContain("There is already", await r.Content.ReadAsStringAsync());
            Assert.Null(await _e.EscalarAsync("SELECT OBJECT_ID(@t, 'U')", ("@t", $"{s.Schema}.A")));
            Assert.Equal("draft", await _e.EscalarAsync("SELECT Status FROM sb.Systems WHERE Id = @id", ("@id", s.Id)));
        }

        [Fact]
        public async Task Eliminar_un_sistema_archiva_sus_datos()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Datos", Pk(), Texto("Valor", 20)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Datos] (Valor) VALUES ('guardado')");

            await EsperarOk(await _e.Admin.DeleteAsync($"/api/v1/sistemas/{s.Id}"), "eliminar sistema");

            Assert.Null(await _e.EscalarAsync("SELECT SCHEMA_ID(@s)", ("@s", s.Schema)));
            var archivado = (string?)await _e.EscalarAsync("SELECT name FROM sys.schemas WHERE name LIKE @p", ("@p", $"{s.Schema}[_]eliminado[_]%"));
            Assert.NotNull(archivado);
            Assert.Equal("guardado", await _e.EscalarAsync($"SELECT Valor FROM [{archivado}].[Datos]"));
        }

        private async Task<int> CampoIdAsync(Sistema s, string entidad, string columna) =>
            Convert.ToInt32(await _e.EscalarAsync("SELECT Id FROM sb.Fields WHERE EntityId = @e AND ColumnName = @c",
                ("@e", s.Entidades[entidad]), ("@c", columna)));

        private async Task EditarLargoAsync(Sistema s, string entidad, int campoId, int largo) =>
            await EsperarOk(await _e.Admin.PutAsJsonAsync($"/api/v1/sistemas/{s.Id}/entidades/{s.Entidades[entidad]}/campos/{campoId}",
                new { name = "Nombre", columnName = "Nombre", dataType = "string", maxLength = largo, sortOrder = 2 }), "editar largo");

        private async Task<int> LargoColumnaAsync(string schema, string tabla, string columna) =>
            Convert.ToInt32(await _e.EscalarAsync(
                "SELECT c.max_length / 2 FROM sys.columns c WHERE c.object_id = OBJECT_ID(@t) AND c.name = @c",
                ("@t", $"{schema}.{tabla}"), ("@c", columna)));
    }
}
