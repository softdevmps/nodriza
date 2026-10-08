using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Tests.Infra;
using static Backend.Tests.Infra.Constructores;

namespace Backend.Tests.Integracion
{
    /// <summary>Renombrar tablas y columnas en el diseño y republicar: los datos tienen que seguir ahí.</summary>
    [Collection(ColeccionFabrica.Nombre)]
    public class RenombresTests
    {
        private readonly EntornoPruebas _e;
        public RenombresTests(EntornoPruebas entorno) => _e = entorno;

        [Fact]
        public async Task Renombrar_un_campo_conserva_sus_datos()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Clientes", Pk(), Texto("Nombre", 80)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Clientes] (Nombre) VALUES (N'Ana')");

            await RenombrarCampoAsync(s, "Clientes", "Nombre", "NombreCompleto");
            await PublicarAsync(_e.Admin, s);

            Assert.Equal(new[] { "Id", "NombreCompleto" }, await ColumnasAsync(s.Schema, "Clientes"));
            Assert.Equal("Ana", await _e.EscalarAsync($"SELECT NombreCompleto FROM [{s.Schema}].[Clientes]"));
            var filas = await (await _e.Admin.GetAsync(s.Datos("Clientes"))).Content.ReadFromJsonAsync<List<JsonElement>>();
            Assert.Equal("Ana", filas!.Single().GetProperty("NombreCompleto").GetString());
        }

        [Fact]
        public async Task Renombrar_tabla_y_FK_conserva_datos_relacion_e_indice_unico()
        {
            var s = await CrearSistemaAsync(_e.Admin,
                new Entidad("Autores", Pk(), Texto("Nombre", 80, unico: true)),
                new Entidad("Libros", Pk(), Texto("Titulo", 80), Entero("AutorId")));
            await EsperarOk(await _e.Admin.PostAsJsonAsync($"/api/v1/sistemas/{s.Id}/relaciones", new
            {
                sourceEntityId = s.Entidades["Libros"], targetEntityId = s.Entidades["Autores"], relationType = "ManyToOne", foreignKey = "AutorId"
            }), "crear relación");
            await PublicarAsync(_e.Admin, s);
            var autor = Convert.ToInt32(await _e.EscalarAsync($"INSERT INTO [{s.Schema}].[Autores] (Nombre) OUTPUT inserted.Id VALUES (N'Borges')"));
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Libros] (Titulo, AutorId) VALUES (N'Ficciones', @a)", ("@a", autor));

            await RenombrarEntidadAsync(s, "Autores", "Escritores");
            await RenombrarCampoAsync(s, "Libros", "AutorId", "EscritorId");
            await PublicarAsync(_e.Admin, s);

            Assert.Null(await _e.EscalarAsync("SELECT OBJECT_ID(@t, 'U')", ("@t", $"{s.Schema}.Autores")));
            Assert.Equal("Borges", await _e.EscalarAsync($"SELECT Nombre FROM [{s.Schema}].[Escritores]"));
            Assert.Equal(autor, Convert.ToInt32(await _e.EscalarAsync($"SELECT EscritorId FROM [{s.Schema}].[Libros]")));
            Assert.Equal("EscritorId", await _e.EscalarAsync("SELECT ForeignKey FROM sb.Relations WHERE SourceEntityId = @e", ("@e", s.Entidades["Libros"])));

            // Una sola FK y un solo índice único, con los nombres nuevos (no quedan duplicados con el nombre viejo)
            Assert.Equal(new[] { $"FK_{s.Schema}_Libros_Escritores_EscritorId" }, await NombresAsync(
                "SELECT name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(@t)", $"{s.Schema}.Libros"));
            Assert.Equal(new[] { $"UX_{s.Schema}_Escritores_Nombre" }, await NombresAsync(
                "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID(@t) AND name LIKE 'UX[_]%'", $"{s.Schema}.Escritores"));
            await Assert.ThrowsAnyAsync<Exception>(() =>
                _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Escritores] (Nombre) VALUES (N'Borges')"));
            await Assert.ThrowsAnyAsync<Exception>(() =>
                _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Libros] (Titulo, EscritorId) VALUES (N'x', 999999)"));
        }

        [Fact]
        public async Task Intercambiar_los_nombres_de_dos_columnas_conserva_cada_dato_con_su_campo()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Personas", Pk(), Texto("A", 20), Texto("B", 20)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Personas] (A, B) VALUES (N'de A', N'de B')");

            // En el diseño no puede haber dos campos con la misma columna: se pasa por un nombre intermedio
            await RenombrarCampoAsync(s, "Personas", "A", "Tmp");
            await RenombrarCampoAsync(s, "Personas", "B", "A");
            await RenombrarCampoAsync(s, "Personas", "Tmp", "B");
            await PublicarAsync(_e.Admin, s);

            Assert.Equal("de A", await _e.EscalarAsync($"SELECT B FROM [{s.Schema}].[Personas]"));
            Assert.Equal("de B", await _e.EscalarAsync($"SELECT A FROM [{s.Schema}].[Personas]"));
            Assert.Equal(new[] { "A", "B", "Id" }, await ColumnasAsync(s.Schema, "Personas"));
        }

        [Fact]
        public async Task Si_el_nombre_nuevo_ya_lo_usa_otra_columna_se_rechaza_sin_tocar_nada()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Fichas", Pk(), Texto("Nombre", 40)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Fichas] (Nombre) VALUES (N'dato')");
            await _e.EjecutarAsync($"ALTER TABLE [{s.Schema}].[Fichas] ADD Ocupada NVARCHAR(10) NULL");

            await RenombrarCampoAsync(s, "Fichas", "Nombre", "Ocupada");
            var r = await _e.Admin.PostAsync($"/api/v1/sistemas/{s.Id}/publicar", null);

            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Contains("Ocupada", await r.Content.ReadAsStringAsync());
            Assert.Equal("dato", await _e.EscalarAsync($"SELECT Nombre FROM [{s.Schema}].[Fichas]"));
        }

        [Fact]
        public async Task Una_tabla_publicada_sin_etiquetas_se_etiqueta_y_despues_se_puede_renombrar()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Viejas", Pk(), Texto("Dato", 40)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Viejas] (Dato) VALUES (N'previo')");
            // Como si se hubiera publicado con una versión anterior de la fábrica
            await _e.EjecutarAsync($@"
EXEC sys.sp_dropextendedproperty N'nodriza_entidad', 'SCHEMA', '{s.Schema}', 'TABLE', 'Viejas';
EXEC sys.sp_dropextendedproperty N'nodriza_campo', 'SCHEMA', '{s.Schema}', 'TABLE', 'Viejas', 'COLUMN', 'Id';
EXEC sys.sp_dropextendedproperty N'nodriza_campo', 'SCHEMA', '{s.Schema}', 'TABLE', 'Viejas', 'COLUMN', 'Dato';");

            await PublicarAsync(_e.Admin, s); // reconoce por nombre y etiqueta
            await RenombrarEntidadAsync(s, "Viejas", "Nuevas");
            await RenombrarCampoAsync(s, "Viejas", "Dato", "Valor");
            await PublicarAsync(_e.Admin, s);

            Assert.Equal("previo", await _e.EscalarAsync($"SELECT Valor FROM [{s.Schema}].[Nuevas]"));
        }

        [Fact]
        public async Task Un_campo_nuevo_puede_usar_el_nombre_que_deja_otro_renombrado()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Contactos", Pk(), Texto("Nombre", 40)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"INSERT INTO [{s.Schema}].[Contactos] (Nombre) VALUES (N'Pérez')");

            await RenombrarCampoAsync(s, "Contactos", "Nombre", "Apellido");
            await EsperarOk(await _e.Admin.PostAsJsonAsync($"/api/v1/sistemas/{s.Id}/entidades/{s.Entidades["Contactos"]}/campos",
                new { name = "Nombre", columnName = "Nombre", dataType = "string", maxLength = 40, sortOrder = 3 }), "campo nuevo");
            await PublicarAsync(_e.Admin, s);

            Assert.Equal("Pérez", await _e.EscalarAsync($"SELECT Apellido FROM [{s.Schema}].[Contactos]"));
            Assert.Null(await _e.EscalarAsync($"SELECT Nombre FROM [{s.Schema}].[Contactos]"));
        }

        [Fact]
        public async Task El_preview_muestra_el_renombrado_y_el_SQL_sin_aplicar_nada()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Socios", Pk(), Texto("Nombre", 40)), new Entidad("Cuotas", Pk()));
            await PublicarAsync(_e.Admin, s);
            await RenombrarCampoAsync(s, "Socios", "Nombre", "NombreCompleto");
            await EsperarOk(await _e.Admin.DeleteAsync($"/api/v1/sistemas/{s.Id}/entidades/{s.Entidades["Cuotas"]}?dropTable=true"), "borrar entidad");
            await EsperarOk(await _e.Admin.PostAsJsonAsync($"/api/v1/sistemas/{s.Id}/entidades", new { name = "Pagos", tableName = "Pagos", sortOrder = 3 }), "entidad nueva");
            var pagos = Convert.ToInt32(await _e.EscalarAsync("SELECT Id FROM sb.Entities WHERE SystemId = @s AND Name = 'Pagos'", ("@s", s.Id)));
            await EsperarOk(await _e.Admin.PostAsJsonAsync($"/api/v1/sistemas/{s.Id}/entidades/{pagos}/campos", Pk()), "pk de pagos");

            var r = await _e.Admin.GetAsync($"/api/v1/sistemas/{s.Id}/publicar/preview");
            await EsperarOk(r, "preview");
            var p = await r.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(p.GetProperty("ok").GetBoolean());
            Assert.Equal(new[] { "Socios.Nombre → NombreCompleto" }, Lista(p, "renombres"));
            Assert.Equal(new[] { "Pagos" }, Lista(p, "tablasNuevas"));
            Assert.Contains(Lista(p, "cambios"), c => c.Contains("sp_rename") && c.Contains("NombreCompleto"));
            Assert.Equal(new[] { "Id", "Nombre" }, await ColumnasAsync(s.Schema, "Socios")); // nada se aplicó
            Assert.Null(await _e.EscalarAsync("SELECT OBJECT_ID(@t, 'U')", ("@t", $"{s.Schema}.Pagos")));
        }

        [Fact]
        public async Task El_preview_informa_lo_que_impediria_publicar()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Fichas", Pk(), Texto("Nombre", 40)));
            await PublicarAsync(_e.Admin, s);
            await _e.EjecutarAsync($"ALTER TABLE [{s.Schema}].[Fichas] ADD Ocupada NVARCHAR(10) NULL");
            await RenombrarCampoAsync(s, "Fichas", "Nombre", "Ocupada");

            var p = await (await _e.Admin.GetAsync($"/api/v1/sistemas/{s.Id}/publicar/preview")).Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(p.GetProperty("ok").GetBoolean());
            Assert.Contains(Lista(p, "errores"), e => e.Contains("Ocupada"));
            var builds = Convert.ToInt32(await _e.EscalarAsync("SELECT COUNT(*) FROM sb.SystemBuilds WHERE SystemId = @s AND Status = 'failed'", ("@s", s.Id)));
            Assert.Equal(0, builds); // el preview no registra intentos fallidos
        }

        [Fact]
        public async Task El_preview_es_solo_para_administradores()
        {
            var s = await CrearSistemaAsync(_e.Admin, new Entidad("Privada", Pk()));
            var comun = await UsuarioComunAsync(_e);
            Assert.Equal(HttpStatusCode.Forbidden, (await comun.GetAsync($"/api/v1/sistemas/{s.Id}/publicar/preview")).StatusCode);
        }

        private static string[] Lista(JsonElement e, string propiedad) =>
            e.GetProperty(propiedad).EnumerateArray().Select(x => x.GetString()!).ToArray();

        // ---------- ayudas ----------

        /// <summary>Edita el campo como lo haría la UI: mismos datos, otro nombre de columna.</summary>
        private async Task RenombrarCampoAsync(Sistema s, string entidad, string columna, string nueva)
        {
            var entidadId = s.Entidades.TryGetValue(entidad, out var id) ? id : throw new ArgumentException(entidad);
            var r = await _e.Admin.GetAsync($"/api/v1/sistemas/{s.Id}/entidades/{entidadId}/campos");
            await EsperarOk(r, "leer campos");
            var campo = (await r.Content.ReadFromJsonAsync<List<JsonElement>>())!
                .Single(c => c.GetProperty("columnName").GetString() == columna);
            var cuerpo = JsonSerializer.SerializeToNode(campo)!.AsObject();
            cuerpo["name"] = nueva;
            cuerpo["columnName"] = nueva;
            await EsperarOk(await _e.Admin.PutAsJsonAsync(
                $"/api/v1/sistemas/{s.Id}/entidades/{entidadId}/campos/{campo.GetProperty("id").GetInt32()}", cuerpo), $"renombrar {columna}");
        }

        private async Task RenombrarEntidadAsync(Sistema s, string entidad, string tabla) =>
            await EsperarOk(await _e.Admin.PutAsJsonAsync($"/api/v1/sistemas/{s.Id}/entidades/{s.Entidades[entidad]}",
                new { name = entidad, tableName = tabla, sortOrder = 1, isActive = true }), $"renombrar tabla {entidad}");

        private Task<string[]> ColumnasAsync(string schema, string tabla) =>
            NombresAsync("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@t)", $"{schema}.{tabla}");

        private async Task<string[]> NombresAsync(string sql, string tabla)
        {
            var lista = await _e.EscalarAsync($"SELECT STRING_AGG(name, ',') WITHIN GROUP (ORDER BY name) FROM ({sql}) x", ("@t", tabla));
            return lista is string s ? s.Split(',') : Array.Empty<string>();
        }
    }
}
