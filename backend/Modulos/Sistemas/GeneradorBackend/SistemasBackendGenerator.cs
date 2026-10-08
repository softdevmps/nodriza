using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Backend.Comun;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Sistemas.GeneradorBackend.Modelos;

namespace Backend.Modulos.Sistemas.GeneradorBackend
{
    public class SistemasBackendGenerator
    {
        private readonly IDbContextFactory<SystemBaseContext> _contextos;
        private readonly BackendConfigGestor _backendConfigGestor;

        public SistemasBackendGenerator(IDbContextFactory<SystemBaseContext> contextos, BackendConfigGestor backendConfigGestor)
        {
            _contextos = contextos;
            _backendConfigGestor = backendConfigGestor;
        }

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

        public BackendGenerateResult Generar(int systemId, string outputRoot, bool overwrite)
        {
            using var context = _contextos.CreateDbContext();

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

            var backendConfig = _backendConfigGestor.ObtenerPorSistema(systemId);
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
            var projectName = $"{Texto.ToPascalCase(slug)}.Backend";

            // Todo lo que se escribe dentro del código generado tiene que ser un identificador
            // o una ruta segura: un nombre con comillas inyectaría código C# que después corre
            // en el servidor al "Iniciar backend". Se valida antes de escribir cualquier archivo.
            var nombresInvalidos = ValidarNombresGenerables(system.Entities, relations, configByEntityId, systemConfig);
            if (nombresInvalidos.Count > 0)
            {
                return new BackendGenerateResult
                {
                    Ok = false,
                    Message = "No se puede generar el backend: hay nombres inválidos. " + string.Join(" ", nombresInvalidos)
                };
            }

            // El .env del sistema (credenciales propias y configuración del equipo) nunca se pisa:
            // ni al negarse a regenerar ni al regenerar con overwrite.
            string? envExistente = null;
            if (Directory.Exists(outputRoot))
            {
                if (!overwrite)
                {
                    return new BackendGenerateResult
                    {
                        Ok = false,
                        Message = $"La carpeta ya existe: {outputRoot}. No se modificó nada. Usa overwrite=true para reemplazar el código (el .env se conserva)."
                    };
                }

                var envPath = Path.Combine(outputRoot, ".env");
                if (File.Exists(envPath))
                    envExistente = File.ReadAllText(envPath);
                Directory.Delete(outputRoot, true);
            }

            Directory.CreateDirectory(outputRoot);
            PrepararEnv(context, outputRoot, slug, envExistente);

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
                var safeName = Texto.ToPascalCase(entity.Name);
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

        private string BuildCsproj(string projectName)
        {
            return Plantillas.Leer("proyecto.csproj");
        }

        private string BuildEnvExample()
        {
            return Plantillas.Leer("env.example");
        }

        private string BuildEnvFile(string slug, string dbUser, string dbPassword, string jwtSecret)
        {
            string Get(string key, string fallback) =>
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key))
                    ? fallback
                    : Environment.GetEnvironmentVariable(key)!;

            return Plantillas.Leer("env",
                ("dbServidor", Get("DB_SERVER", "localhost,1433")),
                ("dbNombre", Get("DB_NAME", "systemBase")),
                ("dbUsuario", dbUser),
                ("dbPassword", dbPassword),
                ("dbTrustCert", Get("DB_TRUST_CERT", "True")),
                ("jwtSecreto", jwtSecret),
                ("slug", slug),
                ("jwtMinutos", Get("JWT_EXPIRE_MINUTES", "120")));
        }

        /// <summary>
        /// Deja el .env del sistema listo y la cuenta SQL del sistema creada con permisos mínimos.
        /// - Si había un .env (regenerar con overwrite) se conserva tal cual y se re-aseguran los permisos SQL.
        /// - Si no había, se crea con credenciales SQL y secreto JWT propios (nunca los de la fábrica).
        /// </summary>
        private void PrepararEnv(SystemBaseContext context, string outputRoot, string slug, string? envExistente)
        {
            var envPath = Path.Combine(outputRoot, ".env");
            File.WriteAllText(Path.Combine(outputRoot, ".env.example"), BuildEnvExample(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outputRoot, ".gitignore"), BuildGitignore(), new UTF8Encoding(false));

            if (envExistente != null)
            {
                File.WriteAllText(envPath, envExistente, new UTF8Encoding(false));
                var valores = envExistente.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => l.Contains('=') && !l.StartsWith('#'))
                    .Select(l => l.Split('=', 2))
                    .ToDictionary(kv => kv[0].Trim(), kv => kv[1].Trim(), StringComparer.OrdinalIgnoreCase);
                if (valores.TryGetValue("DB_USER", out var usuario) && usuario == CredencialesSistema.NombreLogin(slug) &&
                    valores.TryGetValue("DB_PASSWORD", out var pass) && !string.IsNullOrEmpty(pass))
                {
                    CredencialesSistema.AsegurarLogin(context, slug, pass);
                }
                return;
            }

            var password = CredencialesSistema.GenerarPassword();
            CredencialesSistema.AsegurarLogin(context, slug, password);
            File.WriteAllText(envPath, BuildEnvFile(slug, CredencialesSistema.NombreLogin(slug), password, CredencialesSistema.GenerarSecretoJwt()), new UTF8Encoding(false));
        }

        private string BuildGitignore()
        {
            return Plantillas.Leer("gitignore");
        }

        private string BuildProgram()
        {
            return Plantillas.Leer("Program.cs");
        }

        private string BuildLaunchSettings(string projectName, int httpPort)
        {
            return Plantillas.Leer("launchSettings.json", ("proyecto", projectName), ("puerto", httpPort));
        }

        private int GetBackendPort(int systemId)
        {
            return PuertosSistemas.Backend(systemId);
        }

        private void WritePortsRegistry(string outputRoot, SystemBaseContext context)
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
                    basePort = PuertosSistemas.BaseBackend,
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

        private string BuildRoutes(string apiBase, IEnumerable<Entities> entities, Dictionary<int, BackendEntityConfig> configByEntityId)
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
                var name = Texto.ToPascalCase(entity.Name);
                var route = configByEntityId.TryGetValue(entity.Id, out var cfg) && !string.IsNullOrWhiteSpace(cfg.Route)
                    ? cfg.Route
                    : Texto.ToKebab(entity.Name);

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

        private string BuildDevToolsController()
        {
            return Plantillas.Leer("DevToolsController.cs");
        }

        private string BuildDbClass()
        {
            return Plantillas.Leer("Db.cs");
        }

        private string BuildAppConfig()
        {
            return Plantillas.Leer("AppConfig.cs");
        }

        private string BuildJwtService()
        {
            return Plantillas.Leer("JwtService.cs");
        }

        private string BuildAuthGestor()
        {
            return Plantillas.Leer("AuthGestor.cs");
        }

        private string BuildLoginRequest()
        {
            return Plantillas.Leer("LoginRequest.cs");
        }

        private string BuildLoginResponse()
        {
            return Plantillas.Leer("LoginResponse.cs");
        }

        private string BuildRegistrarRequest()
        {
            return Plantillas.Leer("RegistrarRequest.cs");
        }

        private string BuildUsuarioToken()
        {
            return Plantillas.Leer("UsuarioToken.cs");
        }

        private string BuildAppController()
        {
            return Plantillas.Leer("AppController.cs");
        }

        private string BuildAuthController()
        {
            return Plantillas.Leer("AuthController.cs");
        }

        private string BuildEntityModel(string entityName, List<Fields> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend.Models.Entidades");
            sb.AppendLine("{");
            sb.AppendLine("    public class " + entityName);
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                var propertyName = Texto.ToPascalCase(field.ColumnName);
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

        private string BuildEntityResponse(string entityName, List<FieldConfig> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend.Models." + entityName);
            sb.AppendLine("{");
            sb.AppendLine("    public class " + entityName + "Response");
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                var propertyName = Texto.ToPascalCase(field.Field.ColumnName);
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

        private string BuildEntityCreateRequest(string entityName, List<FieldConfig> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend.Models." + entityName);
            sb.AppendLine("{");
            sb.AppendLine("    public class " + entityName + "CreateRequest");
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                var propertyName = Texto.ToPascalCase(field.Field.ColumnName);
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

        private string BuildEntityUpdateRequest(string entityName, List<FieldConfig> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Backend.Models." + entityName);
            sb.AppendLine("{");
            sb.AppendLine("    public class " + entityName + "UpdateRequest");
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                var propertyName = Texto.ToPascalCase(field.Field.ColumnName);
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

        private string BuildEntityGestor(string schema, Entities entity, List<FieldConfig> fieldConfigs, BackendEntityConfig config, BackendSystemConfig systemConfig, List<RelationCheck> relationChecks)
        {
            var entityName = Texto.ToPascalCase(entity.Name);
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
                var prop = Texto.ToPascalCase(field.Field.ColumnName);
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
                var prop = Texto.ToPascalCase(field.Field.ColumnName);
                addInsertParams.AppendLine(BuildParameterLine(field, prop, useDefault: true));
            }

            var addUpdateParams = new StringBuilder();
            foreach (var field in updateFields)
            {
                var prop = Texto.ToPascalCase(field.Field.ColumnName);
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

            // Con paginación, el controller informa el total (sin paginar) en el header X-Total-Count
            var countMethod = config.Pagination
                ? $@"

        public static int ContarTodos(string? search)
        {{
            using var conn = Db.Open();
            using var cmd = new SqlCommand(""SELECT COUNT(*) FROM [{schema}].[{tableName}]{whereSql}"", conn);
{(hasSearch ? "            cmd.Parameters.AddWithValue(\"@search\", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : $\"%{{search}}%\");\n" : string.Empty)}            return (int)cmd.ExecuteScalar();
        }}"
                : string.Empty;

            var createValidation = new StringBuilder();
            var relationByColumn = new Dictionary<string, RelationCheck>(StringComparer.OrdinalIgnoreCase);
            foreach (var rel in relationChecks)
                relationByColumn[rel.ForeignKeyColumn] = rel;
            foreach (var field in insertFields)
            {
                var prop = Texto.ToPascalCase(field.Field.ColumnName);
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
                var prop = Texto.ToPascalCase(field.Field.ColumnName);
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
        }}{countMethod}

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

        private string BuildEntityController(Entities entity, BackendEntityConfig config, BackendSystemConfig systemConfig)
        {
            var entityName = Texto.ToPascalCase(entity.Name);
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
            var items = {listCall};{(config.Pagination ? $@"
            Response.Headers[""X-Total-Count""] = {entityName}Gestor.ContarTodos(search).ToString();" : string.Empty)}
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

        private bool ResolveEndpointAuth(BackendEntityConfig config, BackendSystemConfig systemConfig, bool? endpointOverride)
        {
            return endpointOverride ?? config.RequireAuth ?? systemConfig.RequireAuth;
        }

        private string BuildAuthorizeAttribute(bool requireAuth)
        {
            return requireAuth ? "        [Authorize]\n" : string.Empty;
        }

        private List<RelationCheck> BuildRelationsForEntity(Entities sourceEntity, IEnumerable<Relations> relations, IEnumerable<Entities> allEntities)
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
                    TargetName = EscapeString(targetEntity.Name)
                });
            }

            return list;
        }

        private string MapToCSharpType(Fields field)
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

        private bool IsNullable(Fields field)
        {
            if (field.IsPrimaryKey || field.IsIdentity)
                return false;

            return !field.Required;
        }

        private string BuildParameterLine(FieldConfig field, string propertyName, bool useDefault)
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

        private string? BuildDefaultLiteral(FieldConfig field, string type)
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

        /// <summary>Escapa un texto para usarlo dentro de un literal string C# normal ("...").</summary>
        private string EscapeString(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (char.IsControl(ch))
                            sb.Append($"\\u{(int)ch:x4}");
                        else
                            sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        private List<string> ValidarNombresGenerables(
            IEnumerable<Entities> entities,
            IEnumerable<Relations> relations,
            Dictionary<int, BackendEntityConfig> configByEntityId,
            BackendSystemConfig systemConfig)
        {
            var errores = new List<string>();

            if (!string.IsNullOrWhiteSpace(systemConfig.ApiBase) && !NombresSql.EsRutaValida(systemConfig.ApiBase))
                errores.Add($"ApiBase \"{systemConfig.ApiBase}\": {NombresSql.ReglaRuta}");

            foreach (var entity in entities)
            {
                if (!NombresSql.EsIdentificadorValido(entity.TableName))
                    errores.Add($"Tabla \"{entity.TableName}\" ({entity.Name}): {NombresSql.ReglaIdentificador}");

                foreach (var field in entity.Fields)
                {
                    if (!NombresSql.EsIdentificadorValido(field.ColumnName))
                        errores.Add($"Columna \"{field.ColumnName}\" de {entity.Name}: {NombresSql.ReglaIdentificador}");
                }

                if (configByEntityId.TryGetValue(entity.Id, out var cfg) &&
                    !string.IsNullOrWhiteSpace(cfg.Route) &&
                    !NombresSql.EsRutaValida(cfg.Route))
                {
                    errores.Add($"Ruta \"{cfg.Route}\" de {entity.Name}: {NombresSql.ReglaRuta}");
                }
            }

            foreach (var relation in relations)
            {
                if (!string.IsNullOrWhiteSpace(relation.ForeignKey) && !NombresSql.EsIdentificadorValido(relation.ForeignKey))
                    errores.Add($"FK \"{relation.ForeignKey}\": {NombresSql.ReglaIdentificador}");
            }

            return errores;
        }

        private List<FieldConfig> BuildFieldConfigs(List<Fields> fields, BackendEntityConfig config)
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

        private BackendEntityConfig BuildFallbackEntityConfig(Entities entity, List<Fields> fields)
        {
            var config = new BackendEntityConfig
            {
                EntityId = entity.Id,
                Name = entity.Name,
                DisplayName = entity.DisplayName,
                IsEnabled = true,
                Route = Texto.ToKebab(entity.Name),
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

        private string NormalizeApiBase(string value)
        {
            var trimmed = value?.Trim() ?? "api/v1";
            trimmed = trimmed.Trim('/');
            return string.IsNullOrWhiteSpace(trimmed) ? "api/v1" : trimmed;
        }

    }
}
