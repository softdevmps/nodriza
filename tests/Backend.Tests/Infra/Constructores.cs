using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Backend.Tests.Infra
{
    /// <summary>Atajos para armar datos de prueba a través de la API (como lo haría la UI).</summary>
    public static class Constructores
    {
        private static int _contador;

        public static string Unico(string prefijo) =>
            $"{prefijo}_{Interlocked.Increment(ref _contador)}_{Guid.NewGuid():N}"[..Math.Min(prefijo.Length + 14, 50)].ToLowerInvariant();

        // ---------- Campos ----------
        public static object Pk() => new { name = "Id", columnName = "Id", dataType = "int", isPrimaryKey = true, isIdentity = true, required = true };
        public static object Texto(string col, int largo = 100, bool requerido = false, bool unico = false) =>
            new { name = col, columnName = col, dataType = "string", maxLength = largo, required = requerido, isUnique = unico };
        public static object Entero(string col, bool requerido = false) => new { name = col, columnName = col, dataType = "int", required = requerido };
        public static object Decimal(string col, int precision = 18, int escala = 2) =>
            new { name = col, columnName = col, dataType = "decimal", precision, scale = escala };
        public static object Fecha(string col) => new { name = col, columnName = col, dataType = "datetime" };

        public sealed record Entidad(string Nombre, params object[] Campos);

        public sealed record Sistema(int Id, string Slug, Dictionary<string, int> Entidades)
        {
            public string Schema => $"sys_{Slug}";
            public string Datos(string entidad) => $"/api/v1/sistemas/{Id}/entidades/{Entidades[entidad]}/datos";
        }

        public static async Task<Sistema> CrearSistemaAsync(HttpClient admin, params Entidad[] entidades)
        {
            var slug = Unico("t");
            var r = await admin.PostAsJsonAsync("/api/v1/sistemas", new { slug, name = $"Test {slug}", @namespace = $"Test.{slug}" });
            await EsperarOk(r, "crear sistema");
            var id = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var ids = new Dictionary<string, int>();
            var orden = 1;
            foreach (var entidad in entidades)
            {
                r = await admin.PostAsJsonAsync($"/api/v1/sistemas/{id}/entidades", new { name = entidad.Nombre, tableName = entidad.Nombre, sortOrder = orden++ });
                await EsperarOk(r, $"crear entidad {entidad.Nombre}");
                var entidadId = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
                ids[entidad.Nombre] = entidadId;

                var campoOrden = 1;
                foreach (var campo in entidad.Campos)
                {
                    var json = JsonSerializer.SerializeToNode(campo)!.AsObject();
                    json["sortOrder"] = campoOrden++;
                    r = await admin.PostAsJsonAsync($"/api/v1/sistemas/{id}/entidades/{entidadId}/campos", json);
                    await EsperarOk(r, $"crear campo en {entidad.Nombre}");
                }
            }

            return new Sistema(id, slug, ids);
        }

        public static async Task PublicarAsync(HttpClient admin, Sistema sistema)
        {
            var r = await admin.PostAsync($"/api/v1/sistemas/{sistema.Id}/publicar", null);
            await EsperarOk(r, "publicar");
        }

        /// <summary>Usuario logueado sin ningún permiso (rol nuevo vacío).</summary>
        public static async Task<HttpClient> UsuarioComunAsync(EntornoPruebas entorno)
        {
            var rol = Unico("rol");
            await EsperarOk(await entorno.Admin.PostAsJsonAsync("/api/v1/roles", new { nombre = rol, activo = true }), "crear rol");
            var rolId = Convert.ToInt32(await entorno.EscalarAsync("SELECT Id FROM dbo.Roles WHERE Nombre = @n", ("@n", rol)));
            var (username, password) = (Unico("u"), "Clave-Test-123");
            await EsperarOk(await entorno.Admin.PostAsJsonAsync("/api/v1/usuarios", new
            {
                username, email = $"{username}@test.local", password, nombre = "Test", apellido = "Comun", rolId
            }), "crear usuario");
            return await entorno.LoginAsync(username, password) ?? throw new InvalidOperationException("El usuario común no pudo loguearse");
        }

        public static async Task EsperarOk(HttpResponseMessage r, string accion)
        {
            if (r.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException($"{accion}: HTTP {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        }
    }
}
