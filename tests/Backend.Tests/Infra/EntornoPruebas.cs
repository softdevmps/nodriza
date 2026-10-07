using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace Backend.Tests.Infra
{
    /// <summary>
    /// Levanta la API de la fábrica en memoria contra una base de datos temporal
    /// (nodriza_test_&lt;id&gt;) que se crea al empezar y se borra al terminar.
    ///
    /// Credenciales del servidor SQL: variables DB_SERVER / DB_USER / DB_PASSWORD
    /// (así corre en CI) o, si no están, las de backend/.env (desarrollo local).
    /// Nunca usa DB_NAME: la base de desarrollo no se toca.
    /// </summary>
    public sealed class EntornoPruebas : IAsyncLifetime
    {
        private static readonly Regex PatronBase = new("^nodriza_test_[a-f0-9]{12}$");

        private string _servidor = string.Empty;
        private string _usuario = string.Empty;
        private string _password = string.Empty;

        public string BaseDeDatos { get; } = $"nodriza_test_{Guid.NewGuid():N}"[..25];
        public string AdminPassword { get; } = $"Test-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";
        public WebApplicationFactory<Program> Fabrica { get; private set; } = null!;
        public HttpClient Admin { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            if (!PatronBase.IsMatch(BaseDeDatos))
                throw new InvalidOperationException($"Nombre de base de pruebas inesperado: {BaseDeDatos}");

            (_servidor, _usuario, _password) = LeerCredenciales();
            await CrearBaseAsync();

            Environment.SetEnvironmentVariable("DB_SERVER", _servidor);
            Environment.SetEnvironmentVariable("DB_NAME", BaseDeDatos);
            Environment.SetEnvironmentVariable("DB_USER", _usuario);
            Environment.SetEnvironmentVariable("DB_PASSWORD", _password);
            Environment.SetEnvironmentVariable("DB_TRUST_CERT", "True");
            Environment.SetEnvironmentVariable("JWT_SECRET", Convert.ToHexString(RandomNumberGenerator.GetBytes(48)));
            Environment.SetEnvironmentVariable("JWT_ISSUER", "NodrizaTests");
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", "NodrizaTestsClients");
            Environment.SetEnvironmentVariable("JWT_EXPIRE_MINUTES", "60");
            Environment.SetEnvironmentVariable("ADMIN_PASSWORD", AdminPassword);
            Environment.SetEnvironmentVariable("REGISTRO_PUBLICO", "false");
            // Las vistas autogeneradas de menú escriben en frontend/: en tests no.
            Environment.SetEnvironmentVariable("MENU_VIEW_GENERATOR", "false");

            Fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));
            Admin = await LoginAsync("admin", AdminPassword)
                ?? throw new InvalidOperationException("No se pudo loguear como admin en la fábrica de pruebas.");
        }

        public async Task DisposeAsync()
        {
            Fabrica?.Dispose();
            SqlConnection.ClearAllPools();
            await EjecutarAsync("master", $@"
IF DB_ID(N'{BaseDeDatos}') IS NOT NULL
BEGIN
    ALTER DATABASE [{BaseDeDatos}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{BaseDeDatos}];
END");
        }

        // ---------- HTTP ----------

        public HttpClient Anonimo() => Fabrica.CreateClient();

        /// <summary>Cliente autenticado, o null si el login falla.</summary>
        public async Task<HttpClient?> LoginAsync(string usuario, string password)
        {
            var anon = Fabrica.CreateClient();
            var r = await anon.PostAsJsonAsync("/api/v1/auth/login", new { usuario, password });
            if (!r.IsSuccessStatusCode)
                return null;
            var token = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
            var cliente = Fabrica.CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return cliente;
        }

        // ---------- SQL directo (para verificar el estado real de la base) ----------

        public Task<object?> EscalarAsync(string sql, params (string Nombre, object Valor)[] parametros) =>
            EscalarEnAsync(BaseDeDatos, sql, parametros);

        public async Task EjecutarAsync(string sql, params (string Nombre, object Valor)[] parametros) =>
            await EjecutarAsync(BaseDeDatos, sql, parametros);

        private async Task<object?> EscalarEnAsync(string baseDeDatos, string sql, (string Nombre, object Valor)[] parametros)
        {
            await using var conn = new SqlConnection(Conexion(baseDeDatos));
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            foreach (var (nombre, valor) in parametros)
                cmd.Parameters.AddWithValue(nombre, valor);
            var r = await cmd.ExecuteScalarAsync();
            return r == DBNull.Value ? null : r;
        }

        private async Task EjecutarAsync(string baseDeDatos, string sql, params (string Nombre, object Valor)[] parametros)
        {
            await using var conn = new SqlConnection(Conexion(baseDeDatos));
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            foreach (var (nombre, valor) in parametros)
                cmd.Parameters.AddWithValue(nombre, valor);
            await cmd.ExecuteNonQueryAsync();
        }

        private string Conexion(string baseDeDatos) =>
            new SqlConnectionStringBuilder
            {
                DataSource = _servidor,
                InitialCatalog = baseDeDatos,
                UserID = _usuario,
                Password = _password,
                TrustServerCertificate = true
            }.ConnectionString;

        // ---------- Preparación ----------

        private async Task CrearBaseAsync()
        {
            var sqlDir = Path.Combine(RaizRepo(), "backend", "sql");
            var crear = File.ReadAllText(Path.Combine(sqlDir, "00_create_database.sql")).Replace("$(DB_NAME)", BaseDeDatos);
            await EjecutarScriptAsync("master", crear);
            foreach (var archivo in new[] { "01_base_tables.sql", "systembase_metadata.sql" })
                await EjecutarScriptAsync(BaseDeDatos, File.ReadAllText(Path.Combine(sqlDir, archivo)));
        }

        /// <summary>Ejecuta un script con separadores GO (como sqlcmd) en una sola conexión.</summary>
        private async Task EjecutarScriptAsync(string baseDeDatos, string script)
        {
            await using var conn = new SqlConnection(Conexion(baseDeDatos));
            await conn.OpenAsync();
            foreach (var lote in Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(lote))
                    continue;
                await using var cmd = new SqlCommand(lote, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static (string Servidor, string Usuario, string Password) LeerCredenciales()
        {
            string? Var(string nombre) => Environment.GetEnvironmentVariable(nombre);
            if (!string.IsNullOrWhiteSpace(Var("DB_SERVER")) && !string.IsNullOrWhiteSpace(Var("DB_PASSWORD")))
                return (Var("DB_SERVER")!, Var("DB_USER") ?? "sa", Var("DB_PASSWORD")!);

            var env = Path.Combine(RaizRepo(), "backend", ".env");
            if (!File.Exists(env))
                throw new InvalidOperationException("Definí DB_SERVER/DB_USER/DB_PASSWORD o creá backend/.env para correr los tests de integración.");

            var valores = File.ReadAllLines(env)
                .Select(l => l.Trim())
                .Where(l => l.Contains('=') && !l.StartsWith('#'))
                .Select(l => l.Split('=', 2))
                .ToDictionary(kv => kv[0].Trim(), kv => kv[1].Trim());
            return (valores["DB_SERVER"], valores.GetValueOrDefault("DB_USER", "sa"), valores["DB_PASSWORD"]);
        }

        public static string RaizRepo()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "backend", "sql", "01_base_tables.sql")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
        }
    }

    /// <summary>Todos los tests de integración comparten una sola fábrica y se corren en serie.</summary>
    [CollectionDefinition(Nombre)]
    public sealed class ColeccionFabrica : ICollectionFixture<EntornoPruebas>
    {
        public const string Nombre = "Fabrica";
    }
}
