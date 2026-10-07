using System.Text;
using System.Text.Json;
using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Backend.Negocio.Gestores;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Generadores
{
    public static class SistemasBackendGenerator
    {
        private class FieldConfig
        {
            public Fields Field { get; set; } = null!;
            public BackendFieldConfig Config { get; set; } = null!;
        }

        private class RelationCheck
        {
            public string ForeignKeyColumn { get; set; } = null!;
            public string TargetTable { get; set; } = null!;
            public string TargetPkColumn { get; set; } = null!;
            public string TargetName { get; set; } = null!;
        }

        public static BackendGenerateResult Generar(int systemId, string outputRoot, bool overwrite)
        {
            using var context = new SystemBaseContext();

            var system = context.Systems
                .Include(s => s.Entities)
                    .ThenInclude(e => e.Fields)
                .FirstOrDefault(s => s.Id == systemId);

            if (system == null)
            {
                return new BackendGenerateResult
                {
                    Ok = false,
                    Message = "Sistema no encontrado."
                };
            }

            if (system.Entities.Count == 0)
            {
                return new BackendGenerateResult
                {
                    Ok = false,
                    Message = "El sistema no tiene entidades."
                };
            }

            var backendConfig = BackendConfigGestor.ObtenerPorSistema(systemId);
            var systemConfig = backendConfig.System;
            var configByEntityId = backendConfig.Entities.ToDictionary(e => e.EntityId, e => e);

            var relations = context.Relations
                .Where(r => r.SystemId == systemId)
                .ToList();

            if (!string.Equals(systemConfig.Persistence, "sql", StringComparison.OrdinalIgnoreCase))
            {
                return new BackendGenerateResult
                {
                    Ok = false,
                    Message = "El modo EF Core no esta implementado aun. Cambia a SQL directo."
                };
            }

            var entitiesToGenerate = system.Entities
                .Where(e =>
                {
                    if (configByEntityId.TryGetValue(e.Id, out var cfg))
                        return cfg.IsEnabled;
                    return true;
                })
                .OrderBy(e => e.SortOrder)
                .ThenBy(e => e.Id)
                .ToList();
            if (entitiesToGenerate.Count == 0)
            {
                return new BackendGenerateResult
                {
                    Ok = false,
                    Message = "No hay entidades habilitadas para generar backend."
                };
            }

            var slug = system.Slug.Trim();
            var schemaPrefix = string.IsNullOrWhiteSpace(systemConfig.SchemaPrefix)
                ? "sys"
                : systemConfig.SchemaPrefix.Trim();
            var schema = string.IsNullOrWhiteSpace(schemaPrefix)
                ? slug
                : $"{schemaPrefix}_{slug}";
            var projectName = $"{ToPascalCase(slug)}.Backend";

            if (Directory.Exists(outputRoot))
            {
                if (!overwrite)
                {
                    TryUpdateEnvFiles(outputRoot, systemConfig);
                    return new BackendGenerateResult
                    {
                        Ok = false,
                        Message = $"La carpeta ya existe: {outputRoot}. Se actualizo .env con los JWT/DB actuales. Usa overwrite=true para reemplazar."
                    };
                }

                Directory.Delete(outputRoot, true);
            }

            Directory.CreateDirectory(outputRoot);

            TryUpdateEnvFiles(outputRoot, systemConfig);

            var controllersDir = Path.Combine(outputRoot, "Controllers");
            var modelsDir = Path.Combine(outputRoot, "Models");
            var dataDir = Path.Combine(outputRoot, "Data");
            var utilsDir = Path.Combine(outputRoot, "Utils");
            var negocioDir = Path.Combine(outputRoot, "Negocio");
            var gestoresDir = Path.Combine(negocioDir, "Gestores");
            var modelsAuthDir = Path.Combine(modelsDir, "Auth");
            var modelsJwtDir = Path.Combine(modelsDir, "Jwt");
            var modelsEntidadesDir = Path.Combine(modelsDir, "Entidades");
            var propertiesDir = Path.Combine(outputRoot, "Properties");

            Directory.CreateDirectory(controllersDir);
            Directory.CreateDirectory(modelsDir);
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(utilsDir);
            Directory.CreateDirectory(gestoresDir);
            Directory.CreateDirectory(modelsAuthDir);
            Directory.CreateDirectory(modelsJwtDir);
            Directory.CreateDirectory(modelsEntidadesDir);
            Directory.CreateDirectory(propertiesDir);

            File.WriteAllText(Path.Combine(outputRoot, $"{projectName}.csproj"), BuildCsproj(projectName), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outputRoot, "Program.cs"), BuildProgram(), new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(outputRoot, "Routes.cs"),
                BuildRoutes(systemConfig.ApiBase, entitiesToGenerate, configByEntityId),
                new UTF8Encoding(false)
            );
            var backendPort = GetBackendPort(systemId);
            File.WriteAllText(
                Path.Combine(propertiesDir, "launchSettings.json"),
                BuildLaunchSettings(projectName, backendPort),
                new UTF8Encoding(false)
            );
            WritePortsRegistry(outputRoot, context);

            File.WriteAllText(Path.Combine(dataDir, "Db.cs"), BuildDbClass(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(utilsDir, "AppConfig.cs"), BuildAppConfig(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(modelsJwtDir, "JwtService.cs"), BuildJwtService(), new UTF8Encoding(false));

            File.WriteAllText(Path.Combine(modelsAuthDir, "LoginRequest.cs"), BuildLoginRequest(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(modelsAuthDir, "LoginResponse.cs"), BuildLoginResponse(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(modelsAuthDir, "RegistrarRequest.cs"), BuildRegistrarRequest(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(modelsAuthDir, "UsuarioToken.cs"), BuildUsuarioToken(), new UTF8Encoding(false));

            File.WriteAllText(Path.Combine(controllersDir, "AppController.cs"), BuildAppController(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(controllersDir, "AuthController.cs"), BuildAuthController(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(controllersDir, "DevToolsController.cs"), BuildDevToolsController(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(gestoresDir, "AuthGestor.cs"), BuildAuthGestor(), new UTF8Encoding(false));

            foreach (var entity in entitiesToGenerate)
            {
                var relationChecks = BuildRelationsForEntity(entity, relations, system.Entities);
                var safeName = ToPascalCase(entity.Name);
                var entityFolder = Path.Combine(modelsDir, safeName);
                Directory.CreateDirectory(entityFolder);

                var fields = entity.Fields
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Id)
                    .ToList();

                if (!configByEntityId.TryGetValue(entity.Id, out var entityConfig))
                {
                    entityConfig = BuildFallbackEntityConfig(entity, fields);
                }

                var fieldConfigs = BuildFieldConfigs(fields, entityConfig);
                var responseFields = fieldConfigs.Where(f => f.Config.Expose).ToList();
                var createFields = fieldConfigs.Where(f => f.Config.Expose && !f.Config.ReadOnly && !f.Field.IsIdentity).ToList();
                var updateFields = fieldConfigs.Where(f => f.Config.Expose && !f.Config.ReadOnly && !f.Field.IsPrimaryKey && !f.Field.IsIdentity).ToList();

                File.WriteAllText(Path.Combine(modelsEntidadesDir, $"{safeName}.cs"), BuildEntityModel(safeName, fields), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(entityFolder, $"{safeName}Response.cs"), BuildEntityResponse(safeName, responseFields), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(entityFolder, $"{safeName}CreateRequest.cs"), BuildEntityCreateRequest(safeName, createFields), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(entityFolder, $"{safeName}UpdateRequest.cs"), BuildEntityUpdateRequest(safeName, updateFields), new UTF8Encoding(false));

                var gestorPath = Path.Combine(gestoresDir, $"{safeName}Gestor.cs");
                File.WriteAllText(
                    gestorPath,
                    BuildEntityGestor(schema, entity, fieldConfigs, entityConfig, systemConfig, relationChecks),
                    new UTF8Encoding(false)
                );

                var controllerPath = Path.Combine(controllersDir, $"{safeName}Controller.cs");
                File.WriteAllText(
                    controllerPath,
                    BuildEntityController(entity, entityConfig, systemConfig),
                    new UTF8Encoding(false)
                );
            }

            return new BackendGenerateResult
            {
                Ok = true,
                Message = "Backend generado correctamente.",
                OutputPath = outputRoot
            };
        }

        private static string BuildCsproj(string projectName)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""BCrypt.Net-Next"" Version=""4.0.3"" />
    <PackageReference Include=""DotNetEnv"" Version=""3.1.1"" />
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.JwtBearer"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Data.SqlClient"" Version=""5.2.0"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.6.2"" />
  </ItemGroup>

  <ItemGroup>
    <Watch Include="".restart"" />
  </ItemGroup>

</Project>
";
        }

        private static string BuildEnvExample()
        {
            return @"DB_SERVER=SERVIDOR_BASE_DE_DATOS,PUERTO
DB_NAME=NOMBRE_BASE_DE_DATOS
DB_USER=USUARIO_DE_BASE_DE_DATOS
DB_PASSWORD=CONTRASEÑA_DE_BASE_DE_DATOS
DB_TRUST_CERT=True

JWT_SECRET=CLAVE_SUPER_SECRETA
JWT_ISSUER=systembase
JWT_AUDIENCE=systembase
JWT_EXPIRE_MINUTES=120

## Audio pipeline (local)
AUDIO_STORAGE_PROVIDER=local
AUDIO_STORAGE_ROOT=storage/audio
AUDIO_ALLOWED_EXT=mp3,wav,m4a,ogg,opus,webm,aac
AUDIO_MAX_MB=50

## Transcoding (ffmpeg)
AUDIO_TRANSCODE_ENABLED=false
AUDIO_TRANSCODE_FORMAT=opus
AUDIO_TRANSCODE_BITRATE=32k
AUDIO_TRANSCODE_DELETE_ORIGINAL=true
FFMPEG_PATH=ffmpeg

## Retention policies
AUDIO_RETENTION_SOFT_DAYS=0
AUDIO_RETENTION_PURGE_DAYS=0
AUDIO_RETENTION_RUN_MINUTES=60
";
        }

        private static string BuildEnvFile(BackendSystemConfig systemConfig)
        {
            string Get(string key, string fallback) =>
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key))
                    ? fallback
                    : Environment.GetEnvironmentVariable(key)!;

            var dbServer = Get("DB_SERVER", "localhost,1433");
            var dbName = Get("DB_NAME", "systemBase");
            var dbUser = Get("DB_USER", "sa");
            var dbPassword = Get("DB_PASSWORD", "Password123!");
            var dbTrust = Get("DB_TRUST_CERT", "True");

            var jwtSecret = Get("JWT_SECRET", "secret");
            var jwtIssuer = Get("JWT_ISSUER", "systembase");
            var jwtAudience = Get("JWT_AUDIENCE", "systembase");
            var jwtExpire = Get("JWT_EXPIRE_MINUTES", "120");

            var audioProvider = string.IsNullOrWhiteSpace(systemConfig.AudioStorageProvider)
                ? "local"
                : systemConfig.AudioStorageProvider;
            var audioStorageRoot = Get("AUDIO_STORAGE_ROOT", "storage/audio");
            var audioAllowedExt = Get("AUDIO_ALLOWED_EXT", "mp3,wav,m4a,ogg,opus,webm,aac");
            var audioMaxMb = Get("AUDIO_MAX_MB", "50");

            var transcodeEnabled = systemConfig.AudioTranscodeEnabled ? "true" : "false";
            var transcodeFormat = string.IsNullOrWhiteSpace(systemConfig.AudioTranscodeFormat)
                ? "opus"
                : systemConfig.AudioTranscodeFormat;
            var transcodeBitrate = string.IsNullOrWhiteSpace(systemConfig.AudioTranscodeBitrate)
                ? "32k"
                : systemConfig.AudioTranscodeBitrate;
            var transcodeDeleteOriginal = systemConfig.AudioTranscodeDeleteOriginal ? "true" : "false";
            var ffmpegPath = Get("FFMPEG_PATH", "ffmpeg");

            var retentionSoftDays = systemConfig.AudioRetentionSoftDays.ToString();
            var retentionPurgeDays = systemConfig.AudioRetentionPurgeDays.ToString();
            var retentionRunMinutes = systemConfig.AudioRetentionRunMinutes.ToString();

            return $@"DB_SERVER={dbServer}
DB_NAME={dbName}
DB_USER={dbUser}
DB_PASSWORD={dbPassword}
DB_TRUST_CERT={dbTrust}

JWT_SECRET={jwtSecret}
JWT_ISSUER={jwtIssuer}
JWT_AUDIENCE={jwtAudience}
JWT_EXPIRE_MINUTES={jwtExpire}

## Audio pipeline (local)
AUDIO_STORAGE_PROVIDER={audioProvider}
AUDIO_STORAGE_ROOT={audioStorageRoot}
AUDIO_ALLOWED_EXT={audioAllowedExt}
AUDIO_MAX_MB={audioMaxMb}

## Transcoding (ffmpeg)
AUDIO_TRANSCODE_ENABLED={transcodeEnabled}
AUDIO_TRANSCODE_FORMAT={transcodeFormat}
AUDIO_TRANSCODE_BITRATE={transcodeBitrate}
AUDIO_TRANSCODE_DELETE_ORIGINAL={transcodeDeleteOriginal}
FFMPEG_PATH={ffmpegPath}

## Retention policies
AUDIO_RETENTION_SOFT_DAYS={retentionSoftDays}
AUDIO_RETENTION_PURGE_DAYS={retentionPurgeDays}
AUDIO_RETENTION_RUN_MINUTES={retentionRunMinutes}
";
        }

        private static void TryUpdateEnvFiles(string outputRoot, BackendSystemConfig systemConfig)
        {
            try
            {
                File.WriteAllText(Path.Combine(outputRoot, ".env"), BuildEnvFile(systemConfig), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(outputRoot, ".env.example"), BuildEnvExample(), new UTF8Encoding(false));
            }
            catch
            {
                // no-op
            }
        }

        private static string BuildProgram()
        {
            return @"using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Backend.Utils;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(""AllowFrontend"", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = AppConfig.JWT_ISSUER,
            ValidAudience = AppConfig.JWT_AUDIENCE,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(AppConfig.JWT_SECRET)
            )
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition(""Bearer"", new OpenApiSecurityScheme
    {
        Name = ""Authorization"",
        Type = SecuritySchemeType.ApiKey,
        Scheme = ""Bearer"",
        BearerFormat = ""JWT"",
        In = ParameterLocation.Header,
        Description = ""Bearer {token}""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = ""Bearer""
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(""AllowFrontend"");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
";
        }

        private static string BuildLaunchSettings(string projectName, int httpPort)
        {
            return $@"{{
  ""profiles"": {{
    ""{projectName}"": {{
      ""commandName"": ""Project"",
      ""dotnetRunMessages"": true,
      ""launchBrowser"": true,
      ""launchUrl"": ""swagger"",
      ""applicationUrl"": ""http://localhost:{httpPort}"",
      ""environmentVariables"": {{
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }}
    }}
  }}
}}
";
        }

        private static int GetBackendPort(int systemId)
        {
            return 5032 + systemId;
        }

        private static void WritePortsRegistry(string outputRoot, SystemBaseContext context)
        {
            try
            {
                var systemDir = Directory.GetParent(outputRoot)?.FullName;
                var systemsRoot = systemDir != null ? Directory.GetParent(systemDir)?.FullName : null;
                if (string.IsNullOrWhiteSpace(systemsRoot))
                    return;

                var systems = context.Systems
                    .OrderBy(s => s.Id)
                    .Select(s => new
                    {
                        id = s.Id,
                        slug = s.Slug,
                        name = s.Name,
                        port = GetBackendPort(s.Id),
                        baseUrl = $"http://localhost:{GetBackendPort(s.Id)}"
                    })
                    .ToList();

                var payload = new
                {
                    generatedAt = DateTime.UtcNow,
                    basePort = 5032,
                    systems
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(Path.Combine(systemsRoot, "ports.json"), json, new UTF8Encoding(false));
            }
            catch
            {
                // no-op
            }
        }

        private static string BuildRoutes(string apiBase, IEnumerable<Entities> entities, Dictionary<int, BackendEntityConfig> configByEntityId)
        {
            var basePath = NormalizeApiBase(apiBase);
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend");
            sb.AppendLine("{");
            sb.AppendLine("    public static class Routes");
            sb.AppendLine("    {");
            sb.AppendLine("        public static class v1");
            sb.AppendLine("        {");
            sb.AppendLine("            public static class Auth");
            sb.AppendLine("            {");
            sb.AppendLine($"                public const string Login = \"{basePath}/auth/login\";");
            sb.AppendLine($"                public const string Registrar = \"{basePath}/auth/registrar\";");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public static class DevTools");
            sb.AppendLine("            {");
            sb.AppendLine($"                public const string Restart = \"{basePath}/dev/restart\";");
            sb.AppendLine($"                public const string Ping = \"{basePath}/dev/ping\";");
            sb.AppendLine("            }");
            sb.AppendLine();

            foreach (var entity in entities.OrderBy(e => e.SortOrder).ThenBy(e => e.Id))
            {
                var name = ToPascalCase(entity.Name);
                var route = configByEntityId.TryGetValue(entity.Id, out var cfg) && !string.IsNullOrWhiteSpace(cfg.Route)
                    ? cfg.Route
                    : ToKebab(entity.Name);

                sb.AppendLine($"            public static class {name}");
                sb.AppendLine("            {");
                sb.AppendLine($"                public const string Obtener = \"{basePath}/{route}\";");
                sb.AppendLine($"                public const string ObtenerPorId = \"{basePath}/{route}/{{id}}\";");
                sb.AppendLine($"                public const string Crear = \"{basePath}/{route}\";");
                sb.AppendLine($"                public const string Editar = \"{basePath}/{route}/{{id}}\";");
                sb.AppendLine($"                public const string Eliminar = \"{basePath}/{route}/{{id}}\";");
                sb.AppendLine("            }");
                sb.AppendLine();
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildDevToolsController()
        {
            return @"using Microsoft.AspNetCore.Authorization;
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

            return Ok(new { message = ""Reiniciando backend..."" });
        }

        [HttpGet(Routes.v1.DevTools.Ping)]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            return Ok(new { status = ""ok"" });
        }

        private void TouchRestartSignal()
        {
            try
            {
                var path = Path.Combine(_env.ContentRootPath, "".restart"");
                System.IO.File.WriteAllText(path, DateTime.UtcNow.ToString(""O""));
            }
            catch
            {
                // no-op
            }
        }
    }
}
";
        }

        private static string BuildDbClass()
        {
            return @"using Microsoft.Data.SqlClient;
using Backend.Utils;

namespace Backend.Data
{
    public static class Db
    {
        public static SqlConnection Open()
        {
            var connectionString =
                $""Server={AppConfig.DB_SERVER};Database={AppConfig.DB_NAME};User Id={AppConfig.DB_USER};Password={AppConfig.DB_PASSWORD};TrustServerCertificate=True;"";

            var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }
    }
}
";
        }

        private static string BuildAppConfig()
        {
            return @"namespace Backend.Utils
{
    public static class AppConfig
    {
        public static string DB_SERVER => Environment.GetEnvironmentVariable(""DB_SERVER"") ?? """";
        public static string DB_NAME => Environment.GetEnvironmentVariable(""DB_NAME"") ?? """";
        public static string DB_USER => Environment.GetEnvironmentVariable(""DB_USER"") ?? """";
        public static string DB_PASSWORD => Environment.GetEnvironmentVariable(""DB_PASSWORD"") ?? """";

        public static string JWT_SECRET => Environment.GetEnvironmentVariable(""JWT_SECRET"") ?? ""secret"";
        public static string JWT_ISSUER => Environment.GetEnvironmentVariable(""JWT_ISSUER"") ?? ""systembase"";
        public static string JWT_AUDIENCE => Environment.GetEnvironmentVariable(""JWT_AUDIENCE"") ?? ""systembase"";
        public static int JWT_EXPIRE_MINUTES =>
            int.TryParse(Environment.GetEnvironmentVariable(""JWT_EXPIRE_MINUTES""), out var minutes)
                ? minutes
                : 120;
    }
}
";
        }

        private static string BuildJwtService()
        {
            return @"using Backend.Utils;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Models.Jwt
{
    public static class JwtService
    {
        public static (string token, DateTime expiracion) GenerarToken(int usuarioId, string usuario)
        {
            var claims = new[]
            {
                new Claim(""usuarioId"", usuarioId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(AppConfig.JWT_SECRET)
            );

            var token = new JwtSecurityToken(
                issuer: AppConfig.JWT_ISSUER,
                audience: AppConfig.JWT_AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(AppConfig.JWT_EXPIRE_MINUTES),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return (
                new JwtSecurityTokenHandler().WriteToken(token),
                DateTime.UtcNow.AddMinutes(AppConfig.JWT_EXPIRE_MINUTES)
            );
        }
    }
}
";
        }

        private static string BuildAuthGestor()
        {
            return @"using Backend.Data;
using Backend.Models.Auth;
using Backend.Models.Jwt;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class AuthGestor
    {
        public static LoginResponse? Login(LoginRequest request)
        {
            using var conn = Db.Open();

            const string sql = @""SELECT TOP 1 Id, Username, PasswordHash
                                 FROM dbo.Usuarios
                                 WHERE Activo = 1 AND (Username = @u OR Email = @u)"";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(""@u"", request.Usuario);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var id = Convert.ToInt32(reader[""Id""]);
            var username = reader[""Username""].ToString() ?? """";
            var hash = reader[""PasswordHash""].ToString() ?? """";

            if (!BCrypt.Net.BCrypt.Verify(request.Password, hash))
                return null;

            var (token, expiracion) = JwtService.GenerarToken(id, username);

            return new LoginResponse
            {
                UsuarioId = id,
                Usuario = username,
                Token = token,
                Expiracion = expiracion
            };
        }

        public static bool Registrar(RegistrarRequest model)
        {
            using var conn = Db.Open();

            const string sqlExiste = @""SELECT COUNT(1)
                                       FROM dbo.Usuarios
                                       WHERE Username = @u OR Email = @e"";

            using var cmdExiste = new SqlCommand(sqlExiste, conn);
            cmdExiste.Parameters.AddWithValue(""@u"", model.Username);
            cmdExiste.Parameters.AddWithValue(""@e"", model.Email);

            var existe = Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0;
            if (existe)
                return false;

            const string sqlInsert = @""INSERT INTO dbo.Usuarios
                                      (Username, Email, PasswordHash, Nombre, Apellido, Activo, FechaCreacion)
                                      VALUES (@u, @e, @p, @n, @a, 1, GETUTCDATE())"";

            using var cmdInsert = new SqlCommand(sqlInsert, conn);
            cmdInsert.Parameters.AddWithValue(""@u"", model.Username);
            cmdInsert.Parameters.AddWithValue(""@e"", model.Email);
            cmdInsert.Parameters.AddWithValue(""@p"", BCrypt.Net.BCrypt.HashPassword(model.Password));
            cmdInsert.Parameters.AddWithValue(""@n"", model.Nombre);
            cmdInsert.Parameters.AddWithValue(""@a"", model.Apellido);
            cmdInsert.ExecuteNonQuery();

            return true;
        }
    }
}
";
        }

        private static string BuildLoginRequest()
        {
            return @"namespace Backend.Models.Auth
{
    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
";
        }

        private static string BuildLoginResponse()
        {
            return @"namespace Backend.Models.Auth
{
    public class LoginResponse
    {
        public int UsuarioId { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime Expiracion { get; set; }
    }
}
";
        }

        private static string BuildRegistrarRequest()
        {
            return @"using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Auth
{
    public class RegistrarRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Apellido { get; set; } = string.Empty;
    }
}
";
        }

        private static string BuildUsuarioToken()
        {
            return @"namespace Backend.Models.Auth
{
    public class UsuarioToken
    {
        public int UsuarioId { get; set; }
        public string? Usuario { get; set; }
    }
}
";
        }

        private static string BuildAppController()
        {
            return @"using Backend.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    public class AppController : ControllerBase
    {
        protected UsuarioToken UsuarioToken()
        {
            var usuarioId = User.FindFirst(""usuarioId"")?.Value;
            var usuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return new UsuarioToken
            {
                UsuarioId = usuarioId != null ? int.Parse(usuarioId) : 0,
                Usuario = usuario
            };
        }
    }
}
";
        }

        private static string BuildAuthController()
        {
            return @"using Backend.Models.Auth;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class AuthController : AppController
    {
        [HttpPost(Routes.v1.Auth.Registrar)]
        public IActionResult Registrar([FromBody] RegistrarRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(""Datos inválidos"");

            var ok = AuthGestor.Registrar(model);
            if (!ok)
                return BadRequest(""Usuario o email ya existente"");

            return Ok(""Usuario creado correctamente"");
        }

        [HttpPost(Routes.v1.Auth.Login)]
        public IActionResult Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(""Datos incorrectos"");

            var result = AuthGestor.Login(model);
            if (result == null)
                return Unauthorized(""Usuario o contraseña incorrectos"");

            return Ok(result);
        }
    }
}
";
        }

        private static string BuildEntityModel(string entityName, List<Fields> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend.Models.Entidades");
            sb.AppendLine("{");
            sb.AppendLine("    public class " + entityName);
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                var propertyName = ToPascalCase(field.ColumnName);
                var type = MapToCSharpType(field);
                var nullable = IsNullable(field) ? "?" : "";
                var typeDecl = type == "string"
                    ? (IsNullable(field) ? "string?" : "string")
                    : type + nullable;

                sb.AppendLine($"        public {typeDecl} {propertyName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildEntityResponse(string entityName, List<FieldConfig> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend.Models." + entityName);
            sb.AppendLine("{");
            sb.AppendLine("    public class " + entityName + "Response");
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                var propertyName = ToPascalCase(field.Field.ColumnName);
                var type = MapToCSharpType(field.Field);
                var nullable = IsNullable(field.Field) ? "?" : "";
                var typeDecl = type == "string"
                    ? (IsNullable(field.Field) ? "string?" : "string")
                    : type + nullable;

                sb.AppendLine($"        public {typeDecl} {propertyName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildEntityCreateRequest(string entityName, List<FieldConfig> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend.Models." + entityName);
            sb.AppendLine("{");
            sb.AppendLine("    public class " + entityName + "CreateRequest");
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                var propertyName = ToPascalCase(field.Field.ColumnName);
                var type = MapToCSharpType(field.Field);
                var nullable = IsNullable(field.Field) ? "?" : "";
                var typeDecl = type == "string"
                    ? (IsNullable(field.Field) ? "string?" : "string")
                    : type + nullable;

                if (field.Config.Required == true)
                    sb.AppendLine("        [System.ComponentModel.DataAnnotations.Required]");
                if (field.Config.MaxLength.HasValue && type == "string")
                    sb.AppendLine($"        [System.ComponentModel.DataAnnotations.MaxLength({field.Config.MaxLength.Value})]");

                sb.AppendLine($"        public {typeDecl} {propertyName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildEntityUpdateRequest(string entityName, List<FieldConfig> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend.Models." + entityName);
            sb.AppendLine("{");
            sb.AppendLine("    public class " + entityName + "UpdateRequest");
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                var propertyName = ToPascalCase(field.Field.ColumnName);
                var type = MapToCSharpType(field.Field);
                var nullable = IsNullable(field.Field) ? "?" : "";
                var typeDecl = type == "string"
                    ? (IsNullable(field.Field) ? "string?" : "string")
                    : type + nullable;

                if (field.Config.Required == true)
                    sb.AppendLine("        [System.ComponentModel.DataAnnotations.Required]");
                if (field.Config.MaxLength.HasValue && type == "string")
                    sb.AppendLine($"        [System.ComponentModel.DataAnnotations.MaxLength({field.Config.MaxLength.Value})]");

                sb.AppendLine($"        public {typeDecl} {propertyName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildEntityGestor(string schema, Entities entity, List<FieldConfig> fieldConfigs, BackendEntityConfig config, BackendSystemConfig systemConfig, List<RelationCheck> relationChecks)
        {
            var entityName = ToPascalCase(entity.Name);
            var tableName = entity.TableName;

            var pk = fieldConfigs.FirstOrDefault(f => f.Field.IsPrimaryKey) ?? fieldConfigs.First();
            var pkType = MapToCSharpType(pk.Field);

            var selectFields = fieldConfigs.Where(f => f.Config.Expose).ToList();
            if (selectFields.Count == 0)
                selectFields = fieldConfigs;

            var insertFields = fieldConfigs.Where(f => f.Config.Expose && !f.Config.ReadOnly && !f.Field.IsIdentity).ToList();
            var updateFields = fieldConfigs.Where(f => f.Config.Expose && !f.Config.ReadOnly && !f.Field.IsPrimaryKey && !f.Field.IsIdentity).ToList();

            var selectColumns = string.Join(", ", selectFields.Select(f => $"[{f.Field.ColumnName}]"));
            var insertColumns = string.Join(", ", insertFields.Select(f => $"[{f.Field.ColumnName}]"));
            var insertParams = string.Join(", ", insertFields.Select(f => $"@{f.Field.ColumnName}"));
            var updateSet = string.Join(", ", updateFields.Select(f => $"[{f.Field.ColumnName}] = @{f.Field.ColumnName}"));

            var useSoftDelete = config.SoftDelete;
            if (config.Endpoints?.DeleteConfig?.UseSoftDelete != null)
                useSoftDelete = config.Endpoints.DeleteConfig.UseSoftDelete.Value;

            var softDeleteField = useSoftDelete
                ? fieldConfigs.FirstOrDefault(f => f.Field.Id == config.SoftDeleteFieldId)
                : null;

            var filterIds = config.FilterFieldIds ?? new List<int>();
            var filterFields = fieldConfigs
                .Where(f => filterIds.Contains(f.Field.Id) && MapToCSharpType(f.Field) == "string")
                .ToList();

            var hasSearch = filterFields.Count > 0;
            var sortField = fieldConfigs.FirstOrDefault(f => f.Field.Id == config.DefaultSortFieldId) ?? pk;
            var orderColumn = sortField.Field.ColumnName;
            var sortDirection = string.Equals(config.DefaultSortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : "ASC";

            var mapLines = new StringBuilder();
            foreach (var field in selectFields)
            {
                var prop = ToPascalCase(field.Field.ColumnName);
                var cast = MapToCSharpType(field.Field);
                if (cast == "string")
                {
                    mapLines.AppendLine($"                {prop} = reader[\"{field.Field.ColumnName}\"] == DBNull.Value ? null : reader[\"{field.Field.ColumnName}\"].ToString(),");
                }
                else if (IsNullable(field.Field))
                {
                    mapLines.AppendLine($"                {prop} = reader[\"{field.Field.ColumnName}\"] == DBNull.Value ? null : ({cast})Convert.ChangeType(reader[\"{field.Field.ColumnName}\"], typeof({cast})),");
                }
                else
                {
                    mapLines.AppendLine($"                {prop} = reader[\"{field.Field.ColumnName}\"] == DBNull.Value ? default({cast}) : ({cast})Convert.ChangeType(reader[\"{field.Field.ColumnName}\"], typeof({cast})),");
                }
            }

            var addInsertParams = new StringBuilder();
            foreach (var field in insertFields)
            {
                var prop = ToPascalCase(field.Field.ColumnName);
                addInsertParams.AppendLine(BuildParameterLine(field, prop, useDefault: true));
            }

            var addUpdateParams = new StringBuilder();
            foreach (var field in updateFields)
            {
                var prop = ToPascalCase(field.Field.ColumnName);
                addUpdateParams.AppendLine(BuildParameterLine(field, prop, useDefault: false));
            }

            var insertSql = insertFields.Count > 0
                ? $"INSERT INTO [{schema}].[{tableName}] ({insertColumns}) VALUES ({insertParams});"
                : $"INSERT INTO [{schema}].[{tableName}] DEFAULT VALUES;";

            var createParamsBlock = insertFields.Count > 0
                ? addInsertParams.ToString().TrimEnd()
                : string.Empty;

            var updateBody = updateFields.Count > 0
                ? $@"            var sql = ""UPDATE [{schema}].[{tableName}] SET {updateSet} WHERE [{pk.Field.ColumnName}] = @id"";
            using var cmd = new SqlCommand(sql, conn);
{addUpdateParams.ToString().TrimEnd()}
            cmd.Parameters.AddWithValue(""@id"", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, ""No encontrado"");"
                : "            return (false, \"Sin campos para actualizar\");";

            var searchClause = hasSearch
                ? $"(@search IS NULL OR ({string.Join(" OR ", filterFields.Select(f => $"[{f.Field.ColumnName}] LIKE @search"))}))"
                : string.Empty;

            var whereParts = new List<string>();
            if (softDeleteField != null)
                whereParts.Add($"[{softDeleteField.Field.ColumnName}] = 1");
            if (hasSearch)
                whereParts.Add(searchClause);

            var whereSql = whereParts.Count == 0 ? "" : " WHERE " + string.Join(" AND ", whereParts);

            var paginationSql = config.Pagination
                ? $" ORDER BY [{orderColumn}] {sortDirection} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY"
                : $" ORDER BY [{orderColumn}] {sortDirection}";

            var listMethodSignature = config.Endpoints.List
                ? "public static List<" + entityName + "Response> ObtenerTodos(string? search, int? take, int? skip)"
                : "public static List<" + entityName + "Response> ObtenerTodos(string? search, int? take, int? skip)";

            var listMethodBody = $@"            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append(""SELECT {selectColumns} FROM [{schema}].[{tableName}]"");
            sql.Append(""{whereSql}"");
            sql.Append(""{paginationSql}"");
            using var cmd = new SqlCommand(sql.ToString(), conn);
{(hasSearch ? "            cmd.Parameters.AddWithValue(\"@search\", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : $\"%{{search}}%\");\n" : string.Empty)}{(config.Pagination ? $@"            var takeValue = take ?? {config.DefaultPageSize ?? systemConfig.DefaultPageSize};
            var maxTake = {config.MaxPageSize ?? systemConfig.MaxPageSize};
            if (takeValue > maxTake) takeValue = maxTake;
            if (takeValue < 1) takeValue = {systemConfig.DefaultPageSize};
            var skipValue = skip ?? 0;
            if (skipValue < 0) skipValue = 0;
            cmd.Parameters.AddWithValue(""@take"", takeValue);
            cmd.Parameters.AddWithValue(""@skip"", skipValue);

" : string.Empty)}            using var reader = cmd.ExecuteReader();

            var list = new List<{entityName}Response>();
            while (reader.Read())
            {{
                list.Add(MapToResponse(reader));
            }}

            return list;";

            var createValidation = new StringBuilder();
            var relationByColumn = new Dictionary<string, RelationCheck>(StringComparer.OrdinalIgnoreCase);
            foreach (var rel in relationChecks)
                relationByColumn[rel.ForeignKeyColumn] = rel;
            foreach (var field in insertFields)
            {
                var prop = ToPascalCase(field.Field.ColumnName);
                var type = MapToCSharpType(field.Field);
                if (relationByColumn.TryGetValue(field.Field.ColumnName, out var relation))
                {
                    if (type == "string")
                    {
                        createValidation.AppendLine($"            if (!string.IsNullOrWhiteSpace(request.{prop}) && !ExistsByValue(conn, \"{schema}\", \"{relation.TargetTable}\", \"{relation.TargetPkColumn}\", request.{prop}!, null, null)) return (false, \"{relation.TargetName} inexistente ({relation.ForeignKeyColumn})\");");
                    }
                    else if (IsNullable(field.Field))
                    {
                        createValidation.AppendLine($"            if (request.{prop} != null && !ExistsByValue(conn, \"{schema}\", \"{relation.TargetTable}\", \"{relation.TargetPkColumn}\", request.{prop}!, null, null)) return (false, \"{relation.TargetName} inexistente ({relation.ForeignKeyColumn})\");");
                    }
                    else
                    {
                        createValidation.AppendLine($"            if (!ExistsByValue(conn, \"{schema}\", \"{relation.TargetTable}\", \"{relation.TargetPkColumn}\", request.{prop}, null, null)) return (false, \"{relation.TargetName} inexistente ({relation.ForeignKeyColumn})\");");
                    }
                }
                if (field.Config.Required == true)
                {
                    if (type == "string")
                        createValidation.AppendLine($"            if (string.IsNullOrWhiteSpace(request.{prop})) return (false, \"Campo requerido: {field.Field.ColumnName}\");");
                    else if (IsNullable(field.Field))
                        createValidation.AppendLine($"            if (request.{prop} == null) return (false, \"Campo requerido: {field.Field.ColumnName}\");");
                }

                if (field.Config.MaxLength.HasValue && type == "string")
                    createValidation.AppendLine($"            if (request.{prop} != null && request.{prop}.Length > {field.Config.MaxLength.Value}) return (false, \"MaxLength excedido: {field.Field.ColumnName}\");");

                if (field.Config.Unique == true)
                {
                    string uniqueCheck;
                    if (type == "string")
                    {
                        uniqueCheck = $"            if (!string.IsNullOrWhiteSpace(request.{prop}) && ExistsByValue(conn, \"{schema}\", \"{tableName}\", \"{field.Field.ColumnName}\", request.{prop}!, null, null)) return (false, \"Valor duplicado en {field.Field.ColumnName}\");";
                    }
                    else if (IsNullable(field.Field))
                    {
                        uniqueCheck = $"            if (request.{prop} != null && ExistsByValue(conn, \"{schema}\", \"{tableName}\", \"{field.Field.ColumnName}\", request.{prop}!, null, null)) return (false, \"Valor duplicado en {field.Field.ColumnName}\");";
                    }
                    else
                    {
                        uniqueCheck = $"            if (ExistsByValue(conn, \"{schema}\", \"{tableName}\", \"{field.Field.ColumnName}\", request.{prop}, null, null)) return (false, \"Valor duplicado en {field.Field.ColumnName}\");";
                    }
                    createValidation.AppendLine(uniqueCheck);
                }
            }

            var updateValidation = new StringBuilder();
            foreach (var field in updateFields)
            {
                var prop = ToPascalCase(field.Field.ColumnName);
                var type = MapToCSharpType(field.Field);
                if (relationByColumn.TryGetValue(field.Field.ColumnName, out var relation))
                {
                    if (type == "string")
                    {
                        updateValidation.AppendLine($"            if (!string.IsNullOrWhiteSpace(request.{prop}) && !ExistsByValue(conn, \"{schema}\", \"{relation.TargetTable}\", \"{relation.TargetPkColumn}\", request.{prop}!, null, null)) return (false, \"{relation.TargetName} inexistente ({relation.ForeignKeyColumn})\");");
                    }
                    else if (IsNullable(field.Field))
                    {
                        updateValidation.AppendLine($"            if (request.{prop} != null && !ExistsByValue(conn, \"{schema}\", \"{relation.TargetTable}\", \"{relation.TargetPkColumn}\", request.{prop}!, null, null)) return (false, \"{relation.TargetName} inexistente ({relation.ForeignKeyColumn})\");");
                    }
                    else
                    {
                        updateValidation.AppendLine($"            if (!ExistsByValue(conn, \"{schema}\", \"{relation.TargetTable}\", \"{relation.TargetPkColumn}\", request.{prop}, null, null)) return (false, \"{relation.TargetName} inexistente ({relation.ForeignKeyColumn})\");");
                    }
                }
                if (field.Config.Required == true)
                {
                    if (type == "string")
                        updateValidation.AppendLine($"            if (string.IsNullOrWhiteSpace(request.{prop})) return (false, \"Campo requerido: {field.Field.ColumnName}\");");
                    else if (IsNullable(field.Field))
                        updateValidation.AppendLine($"            if (request.{prop} == null) return (false, \"Campo requerido: {field.Field.ColumnName}\");");
                }

                if (field.Config.MaxLength.HasValue && type == "string")
                    updateValidation.AppendLine($"            if (request.{prop} != null && request.{prop}.Length > {field.Config.MaxLength.Value}) return (false, \"MaxLength excedido: {field.Field.ColumnName}\");");

                if (field.Config.Unique == true)
                {
                    string uniqueCheck;
                    if (type == "string")
                    {
                        uniqueCheck = $"            if (!string.IsNullOrWhiteSpace(request.{prop}) && ExistsByValue(conn, \"{schema}\", \"{tableName}\", \"{field.Field.ColumnName}\", request.{prop}!, \"{pk.Field.ColumnName}\", id)) return (false, \"Valor duplicado en {field.Field.ColumnName}\");";
                    }
                    else if (IsNullable(field.Field))
                    {
                        uniqueCheck = $"            if (request.{prop} != null && ExistsByValue(conn, \"{schema}\", \"{tableName}\", \"{field.Field.ColumnName}\", request.{prop}!, \"{pk.Field.ColumnName}\", id)) return (false, \"Valor duplicado en {field.Field.ColumnName}\");";
                    }
                    else
                    {
                        uniqueCheck = $"            if (ExistsByValue(conn, \"{schema}\", \"{tableName}\", \"{field.Field.ColumnName}\", request.{prop}, \"{pk.Field.ColumnName}\", id)) return (false, \"Valor duplicado en {field.Field.ColumnName}\");";
                    }
                    updateValidation.AppendLine(uniqueCheck);
                }
            }

            var deleteSql = softDeleteField != null
                ? $"UPDATE [{schema}].[{tableName}] SET [{softDeleteField.Field.ColumnName}] = 0 WHERE [{pk.Field.ColumnName}] = @id"
                : $"DELETE FROM [{schema}].[{tableName}] WHERE [{pk.Field.ColumnName}] = @id";

            return $@"using Backend.Data;
using Backend.Models.{entityName};
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{{
    public static class {entityName}Gestor
    {{
        {listMethodSignature}
        {{
{listMethodBody}
        }}

        public static {entityName}Response? ObtenerPorId({pkType} id)
        {{
            using var conn = Db.Open();
            var sql = ""SELECT {selectColumns} FROM [{schema}].[{tableName}] WHERE [{pk.Field.ColumnName}] = @id"";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(""@id"", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }}

        public static (bool Ok, string? Error) Crear({entityName}CreateRequest request)
        {{
            using var conn = Db.Open();
{createValidation.ToString().TrimEnd()}

            var sql = ""{insertSql}"";
            using var cmd = new SqlCommand(sql, conn);
{createParamsBlock}
            cmd.ExecuteNonQuery();
            return (true, null);
        }}

        public static (bool Ok, string? Error) Editar({pkType} id, {entityName}UpdateRequest request)
        {{
            using var conn = Db.Open();
{updateValidation.ToString().TrimEnd()}
{updateBody}
        }}

        public static bool Eliminar({pkType} id)
        {{
            using var conn = Db.Open();
            var sql = ""{deleteSql}"";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(""@id"", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }}

        private static {entityName}Response MapToResponse(SqlDataReader reader)
        {{
            return new {entityName}Response
            {{
{mapLines.ToString().TrimEnd()}
            }};
        }}

        private static bool ExistsByValue(SqlConnection conn, string schema, string table, string column, object value, string? idColumn, object? idValue)
        {{
            var sql = $""SELECT COUNT(1) FROM [{{schema}}].[{{table}}] WHERE [{{column}}] = @val"";
            if (!string.IsNullOrWhiteSpace(idColumn))
                sql += $"" AND [{{idColumn}}] <> @id"";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(""@val"", value);
            if (!string.IsNullOrWhiteSpace(idColumn))
                cmd.Parameters.AddWithValue(""@id"", idValue!);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }}
    }}
}}
";
        }

        private static string BuildEntityController(Entities entity, BackendEntityConfig config, BackendSystemConfig systemConfig)
        {
            var entityName = ToPascalCase(entity.Name);
            var pkField = entity.Fields.FirstOrDefault(f => f.IsPrimaryKey) ?? entity.Fields.First();
            var pkType = MapToCSharpType(pkField);

            var methods = new StringBuilder();

            if (config.Endpoints.List)
            {
                var listAuth = ResolveEndpointAuth(config, systemConfig, config.Endpoints.ListConfig?.RequireAuth);
                var listSignature = (config.Pagination || (config.FilterFieldIds != null && config.FilterFieldIds.Count > 0))
                    ? "public IActionResult Obtener([FromQuery] string? search, [FromQuery] int? take, [FromQuery] int? skip)"
                    : "public IActionResult Obtener()";

                var listCall = (config.Pagination || (config.FilterFieldIds != null && config.FilterFieldIds.Count > 0))
                    ? $"{entityName}Gestor.ObtenerTodos(search, take, skip)"
                    : $"{entityName}Gestor.ObtenerTodos(null, null, null)";

                methods.AppendLine($@"{BuildAuthorizeAttribute(listAuth)}        [HttpGet(Routes.v1.{entityName}.Obtener)]
        {listSignature}
        {{
            var items = {listCall};
            return Ok(items);
        }}
");
            }

            if (config.Endpoints.Get)
            {
                var getAuth = ResolveEndpointAuth(config, systemConfig, config.Endpoints.GetConfig?.RequireAuth);
                methods.AppendLine($@"{BuildAuthorizeAttribute(getAuth)}        [HttpGet(Routes.v1.{entityName}.ObtenerPorId)]
        public IActionResult ObtenerPorId({pkType} id)
        {{
            var item = {entityName}Gestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }}
");
            }

            if (config.Endpoints.Create)
            {
                var createAuth = ResolveEndpointAuth(config, systemConfig, config.Endpoints.CreateConfig?.RequireAuth);
                methods.AppendLine($@"{BuildAuthorizeAttribute(createAuth)}        [HttpPost(Routes.v1.{entityName}.Crear)]
        public IActionResult Crear([FromBody] {entityName}CreateRequest request)
        {{
            var result = {entityName}Gestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }}
");
            }

            if (config.Endpoints.Update)
            {
                var updateAuth = ResolveEndpointAuth(config, systemConfig, config.Endpoints.UpdateConfig?.RequireAuth);
                methods.AppendLine($@"{BuildAuthorizeAttribute(updateAuth)}        [HttpPut(Routes.v1.{entityName}.Editar)]
        public IActionResult Editar({pkType} id, [FromBody] {entityName}UpdateRequest request)
        {{
            var result = {entityName}Gestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }}
");
            }

            if (config.Endpoints.Delete)
            {
                var deleteAuth = ResolveEndpointAuth(config, systemConfig, config.Endpoints.DeleteConfig?.RequireAuth);
                methods.AppendLine($@"{BuildAuthorizeAttribute(deleteAuth)}        [HttpDelete(Routes.v1.{entityName}.Eliminar)]
        public IActionResult Eliminar({pkType} id)
        {{
            var ok = {entityName}Gestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }}
");
            }

            return $@"using Backend.Models.{entityName};
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{{
    [ApiController]
    public class {entityName}Controller : AppController
    {{
{methods.ToString().TrimEnd()}
    }}
}}
";
        }

        private static bool ResolveEndpointAuth(BackendEntityConfig config, BackendSystemConfig systemConfig, bool? endpointOverride)
        {
            return endpointOverride ?? config.RequireAuth ?? systemConfig.RequireAuth;
        }

        private static string BuildAuthorizeAttribute(bool requireAuth)
        {
            return requireAuth ? "        [Authorize]\n" : string.Empty;
        }

        private static List<RelationCheck> BuildRelationsForEntity(Entities sourceEntity, IEnumerable<Relations> relations, IEnumerable<Entities> allEntities)
        {
            var relationsList = relations.ToList();
            var entitiesList = allEntities.ToList();
            if (relationsList.Count == 0 || entitiesList.Count == 0)
                return new List<RelationCheck>();

            var entityById = entitiesList.ToDictionary(e => e.Id, e => e);
            var pkByEntityId = entitiesList.ToDictionary(
                e => e.Id,
                e => e.Fields.FirstOrDefault(f => f.IsPrimaryKey) ?? e.Fields.First()
            );

            var list = new List<RelationCheck>();
            foreach (var rel in relationsList.Where(r => r.SourceEntityId == sourceEntity.Id))
            {
                if (string.IsNullOrWhiteSpace(rel.ForeignKey))
                    continue;

                if (!entityById.TryGetValue(rel.TargetEntityId, out var targetEntity))
                    continue;

                if (!pkByEntityId.TryGetValue(targetEntity.Id, out var targetPk))
                    continue;

                var fkField = sourceEntity.Fields.FirstOrDefault(f =>
                    string.Equals(f.ColumnName, rel.ForeignKey, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Name, rel.ForeignKey, StringComparison.OrdinalIgnoreCase));

                if (fkField == null)
                    continue;

                list.Add(new RelationCheck
                {
                    ForeignKeyColumn = fkField.ColumnName,
                    TargetTable = targetEntity.TableName,
                    TargetPkColumn = targetPk.ColumnName,
                    TargetName = targetEntity.Name
                });
            }

            return list;
        }

        private static string MapToCSharpType(Fields field)
        {
            var type = field.DataType?.ToLowerInvariant();
            return type switch
            {
                "string" => "string",
                "int" => "int",
                "decimal" => "decimal",
                "bool" => "bool",
                "datetime" => "DateTime",
                "guid" => "Guid",
                _ => "string"
            };
        }

        private static bool IsNullable(Fields field)
        {
            if (field.IsPrimaryKey || field.IsIdentity)
                return false;

            return !field.Required;
        }

        private static string BuildParameterLine(FieldConfig field, string propertyName, bool useDefault)
        {
            var type = MapToCSharpType(field.Field);
            var nullable = IsNullable(field.Field);
            var column = field.Field.ColumnName;
            var defaultLiteral = useDefault ? BuildDefaultLiteral(field, type) : null;

            if (type == "string" || nullable)
            {
                if (!string.IsNullOrWhiteSpace(defaultLiteral))
                {
                    return $"            cmd.Parameters.AddWithValue(\"@{column}\", request.{propertyName} ?? {defaultLiteral});";
                }

                return $"            cmd.Parameters.AddWithValue(\"@{column}\", request.{propertyName} ?? (object)DBNull.Value);";
            }

            return $"            cmd.Parameters.AddWithValue(\"@{column}\", request.{propertyName});";
        }

        private static string? BuildDefaultLiteral(FieldConfig field, string type)
        {
            if (string.IsNullOrWhiteSpace(field.Config.DefaultValue))
                return null;

            var value = field.Config.DefaultValue!.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return type switch
            {
                "string" => $"\"{EscapeString(value)}\"",
                "int" => $"int.Parse(\"{EscapeString(value)}\")",
                "decimal" => $"decimal.Parse(\"{EscapeString(value)}\", System.Globalization.CultureInfo.InvariantCulture)",
                "bool" => value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1" ? "true" : "false",
                "datetime" => $"DateTime.Parse(\"{EscapeString(value)}\", System.Globalization.CultureInfo.InvariantCulture)",
                "guid" => $"Guid.Parse(\"{EscapeString(value)}\")",
                _ => $"\"{EscapeString(value)}\""
            };
        }

        private static string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static List<FieldConfig> BuildFieldConfigs(List<Fields> fields, BackendEntityConfig config)
        {
            var configs = config.Fields.ToDictionary(f => f.FieldId, f => f);
            var list = new List<FieldConfig>();

            foreach (var field in fields)
            {
                if (!configs.TryGetValue(field.Id, out var cfg))
                {
                    cfg = new BackendFieldConfig
                    {
                        FieldId = field.Id,
                        Name = field.Name,
                        ColumnName = field.ColumnName,
                        DataType = field.DataType,
                        IsPrimaryKey = field.IsPrimaryKey,
                        IsIdentity = field.IsIdentity,
                        Expose = true,
                        ReadOnly = field.IsIdentity || field.IsPrimaryKey,
                        Required = field.Required,
                        MaxLength = field.MaxLength,
                        Unique = field.IsUnique,
                        DefaultValue = field.DefaultValue
                    };
                }

                list.Add(new FieldConfig
                {
                    Field = field,
                    Config = cfg
                });
            }

            return list;
        }

        private static BackendEntityConfig BuildFallbackEntityConfig(Entities entity, List<Fields> fields)
        {
            var config = new BackendEntityConfig
            {
                EntityId = entity.Id,
                Name = entity.Name,
                DisplayName = entity.DisplayName,
                IsEnabled = true,
                Route = ToKebab(entity.Name),
                RequireAuth = null,
                SoftDelete = false,
                Pagination = false,
                Endpoints = new BackendEndpointsConfig(),
                DefaultSortDirection = "asc",
                FilterFieldIds = new List<int>(),
                Fields = new List<BackendFieldConfig>()
            };

            foreach (var field in fields)
            {
                config.Fields.Add(new BackendFieldConfig
                {
                    FieldId = field.Id,
                    Name = field.Name,
                    ColumnName = field.ColumnName,
                    DataType = field.DataType,
                    IsPrimaryKey = field.IsPrimaryKey,
                    IsIdentity = field.IsIdentity,
                    Expose = true,
                    ReadOnly = field.IsIdentity || field.IsPrimaryKey,
                    Required = field.Required,
                    MaxLength = field.MaxLength,
                    Unique = field.IsUnique,
                    DefaultValue = field.DefaultValue
                });
            }

            return config;
        }

        private static string NormalizeApiBase(string value)
        {
            var trimmed = value?.Trim() ?? "api/v1";
            trimmed = trimmed.Trim('/');
            return string.IsNullOrWhiteSpace(trimmed) ? "api/v1" : trimmed;
        }

        private static string ToPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Item";

            var sb = new StringBuilder();
            var word = new StringBuilder();

            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    word.Append(ch);
                }
                else if (word.Length > 0)
                {
                    AppendWord(sb, word);
                    word.Clear();
                }
            }

            if (word.Length > 0)
                AppendWord(sb, word);

            var result = sb.ToString();
            return string.IsNullOrWhiteSpace(result) ? "Item" : result;
        }

        private static void AppendWord(StringBuilder sb, StringBuilder word)
        {
            var lower = word.ToString().ToLowerInvariant();
            sb.Append(char.ToUpperInvariant(lower[0]));
            if (lower.Length > 1)
                sb.Append(lower[1..]);
        }

        private static string ToKebab(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "item";

            var sb = new StringBuilder();
            var prevDash = false;

            foreach (var ch in value.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (char.IsUpper(ch) && sb.Length > 0 && !prevDash)
                        sb.Append('-');

                    sb.Append(char.ToLowerInvariant(ch));
                    prevDash = false;
                }
                else
                {
                    if (!prevDash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevDash = true;
                    }
                }
            }

            var result = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(result) ? "item" : result;
        }
    }
}
