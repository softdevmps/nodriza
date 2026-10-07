using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Backend.Negocio.Generadores;
using Backend.Negocio.Gestores;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class SistemasController : AppController
    {
        private static readonly ConcurrentDictionary<int, Process> BackendProcesses = new();
        private static readonly ConcurrentDictionary<int, Process> FrontendProcesses = new();
        private static readonly HttpClient Http = new();
        private readonly IWebHostEnvironment _env;

        public SistemasController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet(Routes.v1.Sistemas.Obtener)]
        public IActionResult Obtener()
        {
            var sistemas = SistemasGestor.ObtenerTodos();
            return Ok(sistemas);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var sistema = SistemasGestor.ObtenerPorId(id);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorSlug)]
        public IActionResult ObtenerPorSlug(string slug)
        {
            var sistema = SistemasGestor.ObtenerPorSlug(slug);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpPost(Routes.v1.Sistemas.Crear)]
        public IActionResult Crear([FromBody] SistemaCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = SistemasGestor.Crear(request);
            if (id == null)
                return Conflict("Slug invalido o ya existe.");

            return Ok(new { id });
        }

        [HttpPut(Routes.v1.Sistemas.Editar)]
        public IActionResult Editar(int id, [FromBody] SistemaUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = SistemasGestor.Editar(id, request);
            return ok ? Ok() : NotFound();
        }

        [HttpDelete(Routes.v1.Sistemas.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var result = SistemasGestor.Eliminar(id);
            if (result.NotFound)
                return NotFound();

            if (!result.Ok)
                return BadRequest(new { message = $"No se pudo eliminar el sistema. {result.Error}" });

            TryStopTrackedProcess(BackendProcesses, id);
            TryStopTrackedProcess(FrontendProcesses, id);

            return Ok();
        }

        [HttpPost(Routes.v1.Sistemas.Publicar)]
        public IActionResult Publicar(int id)
        {
            var result = SistemasPublicador.Publicar(id);
            return result.Ok ? Ok(result) : BadRequest(result);
        }

        [HttpPost(Routes.v1.Sistemas.Exportar)]
        public IActionResult Exportar(
            int id,
            [FromQuery] bool full = false,
            [FromQuery] string mode = "zip",
            [FromQuery] bool overwrite = false,
            [FromQuery] string source = "")
        {
            var normalizedMode = (mode ?? "zip").Trim().ToLowerInvariant();
            var normalizedSource = (source ?? string.Empty).Trim().ToLowerInvariant();
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;

            var systemsRoot = Environment.GetEnvironmentVariable("SYSTEMBASE_SYSTEMS_ROOT");
            if (string.IsNullOrWhiteSpace(systemsRoot))
                systemsRoot = Path.Combine(repoRoot, "systems");

            var exportsRoot = Environment.GetEnvironmentVariable("SYSTEMBASE_EXPORT_ROOT");
            if (string.IsNullOrWhiteSpace(exportsRoot))
                exportsRoot = Path.Combine(repoRoot, "exports");

            var preferWorkspaceZip = normalizedMode == "zip" &&
                (string.IsNullOrWhiteSpace(normalizedSource) || normalizedSource == "workspace");

            if (preferWorkspaceZip)
            {
                var sistema = SistemasGestor.ObtenerPorId(id);
                if (sistema == null)
                    return NotFound();

                var workspacePath = Path.Combine(systemsRoot, sistema.Slug);
                if (!Directory.Exists(workspacePath))
                {
                    var exportResult = SistemasExportador.Exportar(
                        id,
                        systemsRoot,
                        _env.ContentRootPath,
                        full,
                        true,
                        overwrite
                    );

                    if (!exportResult.Ok)
                        return BadRequest(exportResult);

                    workspacePath = exportResult.ExportPath;
                }

                var metadataResult = SistemasExportador.ActualizarMetadata(
                    id,
                    workspacePath,
                    _env.ContentRootPath,
                    full
                );

                if (!metadataResult.Ok)
                    return BadRequest(metadataResult);

                Directory.CreateDirectory(exportsRoot);
                var rawName = string.IsNullOrWhiteSpace(sistema.Name) ? sistema.Slug : sistema.Name.Trim();
                var safeName = string.Join("_", rawName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                if (string.IsNullOrWhiteSpace(safeName))
                    safeName = sistema.Slug;
                var zipFileName = $"{safeName}.zip";
                var zipPath = Path.Combine(exportsRoot, zipFileName);
                if (System.IO.File.Exists(zipPath))
                    System.IO.File.Delete(zipPath);

                ZipFile.CreateFromDirectory(workspacePath, zipPath, CompressionLevel.Fastest, false);
                return PhysicalFile(zipPath, "application/zip", zipFileName);
            }

            var exportRoot = normalizedMode == "workspace" ? systemsRoot : exportsRoot;

            var result = SistemasExportador.Exportar(
                id,
                exportRoot,
                _env.ContentRootPath,
                full,
                normalizedMode == "workspace",
                overwrite
            );

            if (!result.Ok)
                return BadRequest(result);

            if (normalizedMode == "workspace")
                return Ok(result);

            if (string.IsNullOrWhiteSpace(result.ZipPath) || string.IsNullOrWhiteSpace(result.ZipFileName))
                return BadRequest(result);

            return PhysicalFile(result.ZipPath, "application/zip", result.ZipFileName);
        }

        [HttpPost(Routes.v1.Sistemas.EjecutarSql)]
        public IActionResult EjecutarSql(int id, [FromBody] SqlScriptExecuteRequest request)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(new { message = "Script SQL invalido." });

            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            if (!IsAdminUser(usuario.UsuarioId))
                return Forbid();

            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var script = NormalizeSqlScript(request.Script);
            if (string.IsNullOrWhiteSpace(script))
                return BadRequest(new { message = "El script SQL esta vacio." });

            var expectedSchema = $"sys_{sistema.Slug}".ToLowerInvariant();
            var validation = ValidateSqlScript(script, expectedSchema);
            if (!validation.Ok)
                return BadRequest(new { message = validation.Error });

            var batches = SplitSqlBatches(script);
            if (batches.Count == 0)
                return BadRequest(new { message = "No se detectaron sentencias SQL ejecutables." });

            using var context = new SystemBaseContext();
            using var trx = context.Database.BeginTransaction();
            try
            {
                var executed = 0;
                foreach (var batch in batches)
                {
                    context.Database.ExecuteSqlRaw(batch);
                    executed++;
                }

                MetadataSyncResult? metadata = null;
                if (request.ImportMetadata)
                    metadata = SyncSchemaMetadata(context, id, expectedSchema);

                var message = $"Script SQL ejecutado. Batches: {executed}.";
                if (metadata != null)
                {
                    message += $" Metadata: entidades +{metadata.EntitiesCreated}/{metadata.EntitiesUpdated}, campos +{metadata.FieldsCreated}/{metadata.FieldsUpdated}, relaciones +{metadata.RelationsCreated}/{metadata.RelationsUpdated}.";
                }

                trx.Commit();
                return Ok(new
                {
                    message,
                    schema = expectedSchema,
                    batches = executed,
                    metadata
                });
            }
            catch (Exception ex)
            {
                trx.Rollback();
                return BadRequest(new { message = $"Error ejecutando SQL: {ex.Message}" });
            }
        }

        [HttpPost(Routes.v1.Sistemas.GenerarBackend)]
        public IActionResult GenerarBackend(int id, [FromQuery] bool overwrite = false)
        {
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var outputRoot = Path.Combine(repoRoot, "systems", sistema.Slug, "backend");
            var result = SistemasBackendGenerator.Generar(id, outputRoot, overwrite);

            if (!result.Ok || string.IsNullOrWhiteSpace(result.OutputPath))
                return BadRequest(result);

            var restoreResult = RunDotnetRestore(outputRoot);
            result.RestoreOk = restoreResult.Ok;
            result.RestoreOutput = restoreResult.Output;
            result.RestoreError = restoreResult.Error;

            return Ok(result);
        }

        [HttpPost(Routes.v1.Sistemas.GenerarFrontend)]
        public IActionResult GenerarFrontend(int id, [FromQuery] bool overwrite = false)
        {
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var frontendSource = Path.Combine(repoRoot, "frontend-runtime");
            var outputRoot = Path.Combine(repoRoot, "systems", sistema.Slug, "frontend");

            var result = SistemasFrontendGenerator.Generar(id, frontendSource, outputRoot, overwrite);
            if (result.Ok)
            {
                FrontendConfigGestor.HabilitarFrontend(id);
                return Ok(result);
            }

            return BadRequest(result);
        }

        [HttpPost(Routes.v1.Sistemas.IniciarBackend)]
        public async Task<IActionResult> IniciarBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var backendPath = Path.Combine(repoRoot, "systems", sistema.Slug, "backend");
            if (!Directory.Exists(backendPath))
                return BadRequest(new { message = "El backend no esta generado. Genera backend primero." });

            if (BackendProcesses.TryGetValue(id, out var existing) && !existing.HasExited)
            {
                BackendProcessLogStore.Add(id, "info", "Inicio solicitado: backend ya en ejecucion.");
                return Ok(new { status = "running", message = "El backend ya esta en ejecucion." });
            }

            if (await IsBackendOnline(id))
            {
                BackendProcessLogStore.Add(id, "info", "Inicio solicitado: backend ya estaba online.");
                return Ok(new { status = "running", message = "El backend ya esta en ejecucion." });
            }

            var logBuffer = BackendProcessLogStore.Reset(id);
            logBuffer.Add("info", "Iniciando backend (dotnet watch run)...");

            var restore = RunDotnetRestore(backendPath);
            if (!restore.Ok)
            {
                logBuffer.Add("error", $"dotnet restore fallo: {restore.Error}");
                return BadRequest(new { message = "dotnet restore fallo. Revisa la consola del backend." });
            }
            logBuffer.Add("info", "dotnet restore ok.");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "watch run",
                WorkingDirectory = backendPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"] = "1";

            var process = Process.Start(startInfo);
            if (process == null)
            {
                logBuffer.Add("error", "No se pudo iniciar el proceso dotnet.");
                return BadRequest(new { message = "No se pudo iniciar el backend." });
            }

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stdout", args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stderr", args.Data);
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.Exited += (_, __) =>
            {
                logBuffer.Add("info", $"dotnet watch terminó (exit code {process.ExitCode}).");
                BackendProcesses.TryRemove(id, out Process removedProcess);
            };

            BackendProcesses[id] = process;
            return Ok(new { status = "started", message = "Backend iniciando..." });
        }

        [HttpPost(Routes.v1.Sistemas.DetenerBackend)]
        public IActionResult DetenerBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!BackendProcesses.TryGetValue(id, out var process) || process.HasExited)
            {
                BackendProcessLogStore.Add(id, "info", "Detener solicitado: backend no estaba en ejecucion.");
                return Ok(new { status = "not_running", message = "El backend no esta en ejecucion." });
            }

            try
            {
                BackendProcessLogStore.Add(id, "info", "Deteniendo backend...");
                process.Kill(true);
                process.WaitForExit(10000);
            }
            catch
            {
                // ignore
            }

            BackendProcesses.TryRemove(id, out Process removedProcess);
            BackendProcessLogStore.Add(id, "info", "Backend detenido.");
            return Ok(new { status = "stopped", message = "Backend detenido." });
        }

        [HttpGet(Routes.v1.Sistemas.PingBackend)]
        public async Task<IActionResult> PingBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var online = await IsBackendOnline(id);
            return Ok(new { online });
        }

        [HttpGet(Routes.v1.Sistemas.LogsBackend)]
        public IActionResult LogsBackend(int id, [FromQuery] long after = 0, [FromQuery] int take = 200)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            take = Math.Clamp(take, 1, 500);
            var buffer = BackendProcessLogStore.Get(id);
            var items = buffer.Read(after, take, out var lastId);
            return Ok(new { items, lastId });
        }

        [HttpPost(Routes.v1.Sistemas.IniciarFrontend)]
        public async Task<IActionResult> IniciarFrontend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var frontendPath = Path.Combine(repoRoot, "systems", sistema.Slug, "frontend");
            if (!Directory.Exists(frontendPath))
                return BadRequest(new { message = "El frontend no esta generado. Genera frontend primero." });

            if (FrontendProcesses.TryGetValue(id, out var existing) && !existing.HasExited)
            {
                FrontendProcessLogStore.Add(id, "info", "Inicio solicitado: frontend ya en ejecucion.");
                return Ok(new { status = "running", message = "El frontend ya esta en ejecucion." });
            }

            if (await IsFrontendOnline(id))
            {
                FrontendProcessLogStore.Add(id, "info", "Inicio solicitado: frontend ya estaba online.");
                return Ok(new { status = "running", message = "El frontend ya esta en ejecucion." });
            }

            var logBuffer = FrontendProcessLogStore.Reset(id);
            logBuffer.Add("info", "Iniciando frontend (npm run dev)...");

            var nodeModules = Path.Combine(frontendPath, "node_modules");
            if (NeedsFrontendInstall(frontendPath, nodeModules))
            {
                if (Directory.Exists(nodeModules))
                    logBuffer.Add("info", "Dependencias incompletas detectadas. Reinstalando (npm install)...");
                else
                    logBuffer.Add("info", "Instalando dependencias (npm install)...");

                var install = RunNpmInstall(frontendPath);
                if (!install.Ok)
                {
                    logBuffer.Add("error", $"npm install fallo: {install.Error}");
                    return BadRequest(new { message = "npm install fallo. Revisa la consola del frontend." });
                }
                logBuffer.Add("info", "npm install ok.");
            }

            var port = GetFrontendPort(id);
            var startInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = $"run dev -- --port {port}",
                WorkingDirectory = frontendPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.Environment["BROWSER"] = "none";
            startInfo.Environment["VITE_PORT"] = port.ToString();

            var process = Process.Start(startInfo);
            if (process == null)
            {
                logBuffer.Add("error", "No se pudo iniciar el proceso npm.");
                return BadRequest(new { message = "No se pudo iniciar el frontend." });
            }

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stdout", args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stderr", args.Data);
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.Exited += (_, __) =>
            {
                logBuffer.Add("info", $"npm dev terminó (exit code {process.ExitCode}).");
                FrontendProcesses.TryRemove(id, out Process removedProcess);
            };

            FrontendProcesses[id] = process;
            return Ok(new { status = "started", message = "Frontend iniciando...", port });
        }

        [HttpPost(Routes.v1.Sistemas.DetenerFrontend)]
        public IActionResult DetenerFrontend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!FrontendProcesses.TryGetValue(id, out var process) || process.HasExited)
            {
                FrontendProcessLogStore.Add(id, "info", "Detener solicitado: frontend no estaba en ejecucion.");
                return Ok(new { status = "not_running", message = "El frontend no esta en ejecucion." });
            }

            try
            {
                FrontendProcessLogStore.Add(id, "info", "Deteniendo frontend...");
                process.Kill(true);
                process.WaitForExit(10000);
            }
            catch
            {
                // ignore
            }

            FrontendProcesses.TryRemove(id, out Process removedProcess);
            FrontendProcessLogStore.Add(id, "info", "Frontend detenido.");
            return Ok(new { status = "stopped", message = "Frontend detenido." });
        }

        [HttpGet(Routes.v1.Sistemas.PingFrontend)]
        public async Task<IActionResult> PingFrontend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var online = await IsFrontendOnline(id);
            return Ok(new { online });
        }

        [HttpGet(Routes.v1.Sistemas.LogsFrontend)]
        public IActionResult LogsFrontend(int id, [FromQuery] long after = 0, [FromQuery] int take = 200)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            take = Math.Clamp(take, 1, 500);
            var buffer = FrontendProcessLogStore.Get(id);
            var items = buffer.Read(after, take, out var lastId);
            return Ok(new { items, lastId });
        }

        private async Task<bool> IsBackendOnline(int systemId)
        {
            try
            {
                var basePath = NormalizeApiBase(BackendConfigGestor.ObtenerPorSistema(systemId)?.System?.ApiBase);
                var port = 5032 + systemId;
                var url = $"http://localhost:{port}{basePath}/dev/ping";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var response = await Http.GetAsync(url, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeApiBase(string? apiBase)
        {
            var value = string.IsNullOrWhiteSpace(apiBase) ? "api/v1" : apiBase.Trim();
            value = value.Trim('/');
            return "/" + value;
        }

        private static int GetFrontendPort(int systemId)
        {
            return 5173 + systemId;
        }

        private async Task<bool> IsFrontendOnline(int systemId)
        {
            try
            {
                var port = GetFrontendPort(systemId);
                var url = $"http://localhost:{port}";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var response = await Http.GetAsync(url, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAdminUser(int usuarioId)
        {
            using var context = new SystemBaseContext();

            return context.Usuarios
                .Include(u => u.Rol)
                .Any(u =>
                    u.Id == usuarioId &&
                    (u.Username != null && u.Username.ToLower() == "admin" ||
                     (u.Rol != null && u.Rol.Nombre.ToLower() == "admin")));
        }

        private static string NormalizeSqlScript(string? script)
        {
            if (string.IsNullOrWhiteSpace(script))
                return string.Empty;

            return script
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Trim();
        }

        private static (bool Ok, string Error) ValidateSqlScript(string script, string expectedSchema)
        {
            if (script.Length > 200_000)
                return (false, "El script excede el tamano permitido (200 KB).");

            if (!script.Contains(expectedSchema, StringComparison.OrdinalIgnoreCase))
                return (false, $"El script debe usar el schema del sistema: {expectedSchema}.");

            var forbiddenPattern = @"\b(use|backup|restore|shutdown|reconfigure|sp_configure|xp_cmdshell|exec\s+xp_|create\s+database|drop\s+database|alter\s+database|create\s+login|drop\s+login|alter\s+login)\b";
            if (Regex.IsMatch(script, forbiddenPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return (false, "El script contiene sentencias no permitidas para esta consola.");

            var forbiddenSchemas = new[] { "dbo", "sb" };
            foreach (var schema in forbiddenSchemas)
            {
                var schemaPattern = $@"(?:\[{schema}\]|{schema})\s*\.";
                if (Regex.IsMatch(script, schemaPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    return (false, $"No se permite operar sobre schema {schema}. Usa solo {expectedSchema}.");
            }

            var sysSchemaMatches = Regex.Matches(
                script,
                @"(?:\[(?<schema>sys_[A-Za-z0-9_]+)\]|(?<schema>sys_[A-Za-z0-9_]+))\s*\.",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            foreach (Match match in sysSchemaMatches)
            {
                var schema = match.Groups["schema"].Value.ToLowerInvariant();
                if (!string.Equals(schema, expectedSchema, StringComparison.OrdinalIgnoreCase))
                    return (false, $"El script referencia un schema distinto ({schema}). Solo se permite {expectedSchema}.");
            }

            var createTableMatches = Regex.Matches(
                script,
                @"\bcreate\s+table\s+(?<name>(?:\[[^\]]+\]|\w+)(?:\s*\.\s*(?:\[[^\]]+\]|\w+))?)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            foreach (Match match in createTableMatches)
            {
                var name = match.Groups["name"].Value;
                if (!TryExtractSchema(name, out var schema))
                    return (false, $"CREATE TABLE debe incluir schema explicito ({expectedSchema}).");

                if (!string.Equals(schema, expectedSchema, StringComparison.OrdinalIgnoreCase))
                    return (false, $"CREATE TABLE fuera del schema permitido ({expectedSchema}).");
            }

            return (true, string.Empty);
        }

        private static bool TryExtractSchema(string value, out string schema)
        {
            schema = string.Empty;
            var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                return false;

            schema = TrimSqlIdentifier(parts[0]).ToLowerInvariant();
            return !string.IsNullOrWhiteSpace(schema);
        }

        private static string TrimSqlIdentifier(string value)
        {
            var trimmed = value.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]") && trimmed.Length >= 2)
                return trimmed[1..^1].Trim();
            return trimmed;
        }

        private static List<string> SplitSqlBatches(string script)
        {
            var chunks = Regex.Split(
                script,
                @"^\s*GO\s*;?\s*$",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant
            );

            return chunks
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private static MetadataSyncResult SyncSchemaMetadata(SystemBaseContext context, int systemId, string schemaName)
        {
            var result = new MetadataSyncResult();
            var runtimeTables = LoadRuntimeTables(context, schemaName);
            if (runtimeTables.Count == 0)
                return result;

            var entities = context.Entities
                .Where(e => e.SystemId == systemId)
                .ToList();

            var entitiesByTable = entities
                .Where(e => !string.IsNullOrWhiteSpace(e.TableName))
                .ToDictionary(e => e.TableName!.ToLowerInvariant(), e => e);

            var nextSortOrder = entities.Count == 0 ? 1 : entities.Max(e => e.SortOrder) + 1;
            var utcNow = DateTime.UtcNow;

            foreach (var table in runtimeTables)
            {
                if (!entitiesByTable.TryGetValue(table.ToLowerInvariant(), out var entity))
                {
                    entity = new Entities
                    {
                        SystemId = systemId,
                        Name = ToPascalIdentifier(table),
                        DisplayName = ToDisplayName(table),
                        TableName = table,
                        SortOrder = nextSortOrder++,
                        IsActive = true,
                        CreatedAt = utcNow
                    };
                    context.Entities.Add(entity);
                    entities.Add(entity);
                    entitiesByTable[table.ToLowerInvariant()] = entity;
                    result.EntitiesCreated++;
                }
                else
                {
                    var changed = false;
                    if (string.IsNullOrWhiteSpace(entity.Name))
                    {
                        entity.Name = ToPascalIdentifier(table);
                        changed = true;
                    }
                    if (string.IsNullOrWhiteSpace(entity.DisplayName))
                    {
                        entity.DisplayName = ToDisplayName(table);
                        changed = true;
                    }
                    if (entity.SortOrder <= 0)
                    {
                        entity.SortOrder = nextSortOrder++;
                        changed = true;
                    }
                    if (changed)
                    {
                        entity.UpdatedAt = utcNow;
                        result.EntitiesUpdated++;
                    }
                }
            }

            context.SaveChanges();

            var syncedEntities = context.Entities
                .Where(e => e.SystemId == systemId && runtimeTables.Contains(e.TableName!))
                .ToList()
                .Where(e => !string.IsNullOrWhiteSpace(e.TableName))
                .ToDictionary(e => e.TableName!.ToLowerInvariant(), e => e);

            var syncedEntityIds = syncedEntities.Values.Select(e => e.Id).ToList();
            var existingFields = context.Fields
                .Where(f => syncedEntityIds.Contains(f.EntityId))
                .ToList();

            var fieldsByKey = existingFields.ToDictionary(
                f => BuildFieldKey(f.EntityId, f.ColumnName),
                f => f
            );

            var runtimeColumns = LoadRuntimeColumns(context, schemaName);
            foreach (var column in runtimeColumns)
            {
                if (!syncedEntities.TryGetValue(column.TableName.ToLowerInvariant(), out var entity))
                    continue;

                var key = BuildFieldKey(entity.Id, column.ColumnName);
                var mappedType = MapSqlTypeToFieldType(column.SqlType);
                var required = !column.IsNullable || column.IsPrimaryKey || column.IsIdentity;
                var maxLength = CalculateMaxLength(column.SqlType, column.MaxLengthBytes);
                var precision = mappedType == "decimal" ? (int?)column.PrecisionValue : null;
                var scale = mappedType == "decimal" ? (int?)column.ScaleValue : null;
                var defaultValue = NormalizeDefaultDefinition(column.DefaultDefinition);

                if (!fieldsByKey.TryGetValue(key, out var field))
                {
                    field = new Fields
                    {
                        EntityId = entity.Id,
                        Name = ToPascalIdentifier(column.ColumnName),
                        ColumnName = column.ColumnName,
                        DataType = mappedType,
                        Required = required,
                        MaxLength = maxLength,
                        Precision = precision,
                        Scale = scale,
                        DefaultValue = defaultValue,
                        IsPrimaryKey = column.IsPrimaryKey,
                        IsIdentity = column.IsIdentity,
                        IsUnique = column.IsUnique,
                        SortOrder = column.ColumnOrder,
                        CreatedAt = utcNow
                    };
                    context.Fields.Add(field);
                    fieldsByKey[key] = field;
                    result.FieldsCreated++;
                }
                else
                {
                    field.Name = string.IsNullOrWhiteSpace(field.Name) ? ToPascalIdentifier(column.ColumnName) : field.Name;
                    field.DataType = mappedType;
                    field.Required = required;
                    field.MaxLength = maxLength;
                    field.Precision = precision;
                    field.Scale = scale;
                    field.DefaultValue = defaultValue;
                    field.IsPrimaryKey = column.IsPrimaryKey;
                    field.IsIdentity = column.IsIdentity;
                    field.IsUnique = column.IsUnique;
                    field.SortOrder = column.ColumnOrder;
                    field.UpdatedAt = utcNow;
                    result.FieldsUpdated++;
                }
            }

            var existingRelations = context.Relations
                .Where(r => r.SystemId == systemId)
                .ToList();

            var relationsByKey = existingRelations.ToDictionary(
                r => BuildRelationKey(r.SourceEntityId, r.TargetEntityId, r.ForeignKey),
                r => r
            );

            var runtimeRelations = LoadRuntimeRelations(context, schemaName);
            foreach (var relation in runtimeRelations)
            {
                if (!syncedEntities.TryGetValue(relation.SourceTableName.ToLowerInvariant(), out var sourceEntity))
                    continue;
                if (!syncedEntities.TryGetValue(relation.TargetTableName.ToLowerInvariant(), out var targetEntity))
                    continue;

                var key = BuildRelationKey(sourceEntity.Id, targetEntity.Id, relation.ForeignKeyColumn);
                if (!relationsByKey.TryGetValue(key, out var existingRelation))
                {
                    var created = new Relations
                    {
                        SystemId = systemId,
                        SourceEntityId = sourceEntity.Id,
                        TargetEntityId = targetEntity.Id,
                        RelationType = "ManyToOne",
                        ForeignKey = relation.ForeignKeyColumn,
                        CascadeDelete = relation.CascadeDelete,
                        CreatedAt = utcNow
                    };
                    context.Relations.Add(created);
                    relationsByKey[key] = created;
                    result.RelationsCreated++;
                }
                else
                {
                    var changed = false;
                    if (!string.Equals(existingRelation.RelationType, "ManyToOne", StringComparison.OrdinalIgnoreCase))
                    {
                        existingRelation.RelationType = "ManyToOne";
                        changed = true;
                    }
                    if (!string.Equals(existingRelation.ForeignKey, relation.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase))
                    {
                        existingRelation.ForeignKey = relation.ForeignKeyColumn;
                        changed = true;
                    }
                    if (existingRelation.CascadeDelete != relation.CascadeDelete)
                    {
                        existingRelation.CascadeDelete = relation.CascadeDelete;
                        changed = true;
                    }

                    if (changed)
                        result.RelationsUpdated++;
                }
            }

            context.SaveChanges();
            return result;
        }

        private static List<string> LoadRuntimeTables(SystemBaseContext context, string schemaName)
        {
            const string sql = @"
SELECT t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @schema
ORDER BY t.name;";

            var tables = new List<string>();
            ExecuteReader(context, sql, cmd =>
            {
                AddParameter(cmd, "@schema", schemaName);
            }, reader =>
            {
                while (reader.Read())
                    tables.Add(reader.GetString(0));
            });
            return tables;
        }

        private static List<RuntimeColumnInfo> LoadRuntimeColumns(SystemBaseContext context, string schemaName)
        {
            const string sql = @"
SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS SqlType,
    c.max_length AS MaxLengthBytes,
    c.[precision] AS PrecisionValue,
    c.scale AS ScaleValue,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity,
    CASE WHEN EXISTS (
        SELECT 1
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.object_id = t.object_id
          AND i.is_primary_key = 1
          AND ic.column_id = c.column_id
    ) THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN EXISTS (
        SELECT 1
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.object_id = t.object_id
          AND i.is_unique = 1
          AND i.is_primary_key = 0
          AND ic.column_id = c.column_id
    ) THEN 1 ELSE 0 END AS IsUnique,
    dc.definition AS DefaultDefinition,
    c.column_id AS ColumnOrder
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
WHERE s.name = @schema
ORDER BY t.name, c.column_id;";

            var columns = new List<RuntimeColumnInfo>();
            ExecuteReader(context, sql, cmd =>
            {
                AddParameter(cmd, "@schema", schemaName);
            }, reader =>
            {
                while (reader.Read())
                {
                    columns.Add(new RuntimeColumnInfo
                    {
                        TableName = reader.GetString(0),
                        ColumnName = reader.GetString(1),
                        SqlType = reader.GetString(2),
                        MaxLengthBytes = reader.GetInt16(3),
                        PrecisionValue = reader.GetByte(4),
                        ScaleValue = reader.GetByte(5),
                        IsNullable = reader.GetBoolean(6),
                        IsIdentity = reader.GetBoolean(7),
                        IsPrimaryKey = reader.GetInt32(8) == 1,
                        IsUnique = reader.GetInt32(9) == 1,
                        DefaultDefinition = reader.IsDBNull(10) ? null : reader.GetString(10),
                        ColumnOrder = reader.GetInt32(11)
                    });
                }
            });
            return columns;
        }

        private static List<RuntimeRelationInfo> LoadRuntimeRelations(SystemBaseContext context, string schemaName)
        {
            const string sql = @"
SELECT
    src_t.name AS SourceTableName,
    src_c.name AS SourceColumnName,
    tgt_t.name AS TargetTableName,
    CAST(CASE WHEN fk.delete_referential_action = 1 THEN 1 ELSE 0 END AS BIT) AS CascadeDelete
FROM sys.foreign_keys fk
INNER JOIN sys.tables src_t ON src_t.object_id = fk.parent_object_id
INNER JOIN sys.schemas src_s ON src_s.schema_id = src_t.schema_id
INNER JOIN sys.tables tgt_t ON tgt_t.object_id = fk.referenced_object_id
INNER JOIN sys.schemas tgt_s ON tgt_s.schema_id = tgt_t.schema_id
INNER JOIN (
    SELECT
        constraint_object_id,
        MIN(parent_column_id) AS ParentColumnId,
        COUNT(*) AS ColumnCount
    FROM sys.foreign_key_columns
    GROUP BY constraint_object_id
) fkc ON fkc.constraint_object_id = fk.object_id AND fkc.ColumnCount = 1
INNER JOIN sys.columns src_c ON src_c.object_id = src_t.object_id AND src_c.column_id = fkc.ParentColumnId
WHERE src_s.name = @schema
  AND tgt_s.name = @schema
ORDER BY src_t.name, src_c.name;";

            var relations = new List<RuntimeRelationInfo>();
            ExecuteReader(context, sql, cmd =>
            {
                AddParameter(cmd, "@schema", schemaName);
            }, reader =>
            {
                while (reader.Read())
                {
                    relations.Add(new RuntimeRelationInfo
                    {
                        SourceTableName = reader.GetString(0),
                        ForeignKeyColumn = reader.GetString(1),
                        TargetTableName = reader.GetString(2),
                        CascadeDelete = reader.GetBoolean(3)
                    });
                }
            });
            return relations;
        }

        private static void ExecuteReader(
            SystemBaseContext context,
            string sql,
            Action<IDbCommand> bindParameters,
            Action<IDataReader> onRead)
        {
            var connection = context.Database.GetDbConnection();
            var openHere = connection.State != ConnectionState.Open;
            if (openHere)
                connection.Open();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                bindParameters(command);
                using var reader = command.ExecuteReader();
                onRead(reader);
            }
            finally
            {
                if (openHere)
                    connection.Close();
            }
        }

        private static void AddParameter(IDbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private static string BuildFieldKey(int entityId, string? columnName)
        {
            return $"{entityId}:{(columnName ?? string.Empty).Trim().ToLowerInvariant()}";
        }

        private static string BuildRelationKey(int sourceEntityId, int targetEntityId, string? foreignKey)
        {
            return $"{sourceEntityId}:{targetEntityId}:{(foreignKey ?? string.Empty).Trim().ToLowerInvariant()}";
        }

        private static string ToPascalIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Item";

            var parts = Regex.Split(value.Trim(), @"[^A-Za-z0-9]+")
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (parts.Count == 0)
                return "Item";

            var output = string.Concat(parts.Select(p =>
            {
                if (p.Length == 1)
                    return p.ToUpperInvariant();
                return char.ToUpperInvariant(p[0]) + p[1..];
            }));

            return char.IsDigit(output[0]) ? $"N{output}" : output;
        }

        private static string ToDisplayName(string value)
        {
            var pascal = ToPascalIdentifier(value);
            return Regex.Replace(pascal, "([a-z0-9])([A-Z])", "$1 $2");
        }

        private static string MapSqlTypeToFieldType(string? sqlType)
        {
            var normalized = (sqlType ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "int" or "bigint" or "smallint" or "tinyint" => "int",
                "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "decimal",
                "bit" => "bool",
                "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" or "time" => "datetime",
                "uniqueidentifier" => "guid",
                _ => "string"
            };
        }

        private static int? CalculateMaxLength(string sqlType, short maxLengthBytes)
        {
            if (maxLengthBytes < 0)
                return null;

            var normalized = (sqlType ?? string.Empty).Trim().ToLowerInvariant();
            var stringTypes = new HashSet<string>
            {
                "char", "varchar", "text", "nchar", "nvarchar", "ntext"
            };

            if (!stringTypes.Contains(normalized))
                return null;

            if (normalized.StartsWith("n"))
                return maxLengthBytes / 2;

            return maxLengthBytes;
        }

        private static string? NormalizeDefaultDefinition(string? definition)
        {
            if (string.IsNullOrWhiteSpace(definition))
                return null;

            var value = definition.Trim();
            while (value.StartsWith("(") && value.EndsWith(")") && value.Length >= 2)
            {
                value = value[1..^1].Trim();
            }

            return value;
        }

        private sealed class RuntimeColumnInfo
        {
            public string TableName { get; set; } = string.Empty;
            public string ColumnName { get; set; } = string.Empty;
            public string SqlType { get; set; } = string.Empty;
            public short MaxLengthBytes { get; set; }
            public byte PrecisionValue { get; set; }
            public byte ScaleValue { get; set; }
            public bool IsNullable { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsPrimaryKey { get; set; }
            public bool IsUnique { get; set; }
            public string? DefaultDefinition { get; set; }
            public int ColumnOrder { get; set; }
        }

        private sealed class RuntimeRelationInfo
        {
            public string SourceTableName { get; set; } = string.Empty;
            public string ForeignKeyColumn { get; set; } = string.Empty;
            public string TargetTableName { get; set; } = string.Empty;
            public bool CascadeDelete { get; set; }
        }

        private sealed class MetadataSyncResult
        {
            public int EntitiesCreated { get; set; }
            public int EntitiesUpdated { get; set; }
            public int FieldsCreated { get; set; }
            public int FieldsUpdated { get; set; }
            public int RelationsCreated { get; set; }
            public int RelationsUpdated { get; set; }
        }

        private (bool Ok, string Output, string Error) RunDotnetRestore(string workingDirectory)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "restore",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                    return (false, string.Empty, "No se pudo iniciar dotnet restore.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(120000))
                {
                    try { process.Kill(true); } catch { }
                    return (false, output, "Timeout ejecutando dotnet restore.");
                }

                var ok = process.ExitCode == 0;
                return (ok, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        private (bool Ok, string Output, string Error) RunNpmInstall(string workingDirectory)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "install",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                    return (false, string.Empty, "No se pudo iniciar npm install.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(240000))
                {
                    try { process.Kill(true); } catch { }
                    return (false, output, "Timeout ejecutando npm install.");
                }

                var ok = process.ExitCode == 0;
                return (ok, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        private static bool NeedsFrontendInstall(string frontendPath, string nodeModulesPath)
        {
            if (!Directory.Exists(nodeModulesPath))
                return true;

            var binPath = Path.Combine(nodeModulesPath, ".bin");
            if (!Directory.Exists(binPath))
                return true;

            var viteCandidates = new[]
            {
                Path.Combine(binPath, "vite"),
                Path.Combine(binPath, "vite.cmd"),
                Path.Combine(binPath, "vite.ps1")
            };

            var hasVite = viteCandidates.Any(System.IO.File.Exists);
            if (!hasVite)
                return true;

            var packageJsonPath = Path.Combine(frontendPath, "package.json");
            return !System.IO.File.Exists(packageJsonPath);
        }

        private static void TryStopTrackedProcess(ConcurrentDictionary<int, Process> processes, int id)
        {
            if (!processes.TryGetValue(id, out var process) || process.HasExited)
            {
                processes.TryRemove(id, out _);
                return;
            }

            try
            {
                process.Kill(true);
                process.WaitForExit(10000);
            }
            catch
            {
                // ignore
            }

            processes.TryRemove(id, out _);
        }
    }
}
