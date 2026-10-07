using System.Collections.Concurrent;
using System.Diagnostics;
using Backend.Comun;
using Backend.Modulos.Sistemas.GeneradorBackend;

namespace Backend.Modulos.Sistemas.Herramientas
{
    public enum Componente
    {
        Backend,
        Frontend
    }

    /// <summary>Resultado de iniciar/detener un proceso, tal como lo devuelve la API.</summary>
    public sealed record ResultadoProceso(bool Ok, string Status, string Message, int? Port = null);

    /// <summary>
    /// Procesos de desarrollo de los sistemas generados (solo DEV):
    /// - Backend: `dotnet watch run` en systems/&lt;slug&gt;/backend (antes hace `dotnet restore`).
    /// - Frontend: `npm run dev` en systems/&lt;slug&gt;/frontend (antes `npm install` si hace falta).
    /// Lleva el registro de los procesos levantados y su salida (ver LogsProcesos).
    /// </summary>
    public static class ProcesosSistemas
    {
        private static readonly ConcurrentDictionary<(Componente, int), Process> Procesos = new();
        private static readonly HttpClient Http = new();

        private static string Nombre(Componente c) => c == Componente.Backend ? "backend" : "frontend";

        public static int Puerto(Componente c, int systemId) =>
            c == Componente.Backend ? PuertosSistemas.Backend(systemId) : PuertosSistemas.Frontend(systemId);

        public static async Task<ResultadoProceso> IniciarAsync(Componente componente, int systemId, string carpeta)
        {
            var nombre = Nombre(componente);
            if (!Directory.Exists(carpeta))
                return new(false, "error", $"El {nombre} no esta generado. Genera {nombre} primero.");

            if ((Procesos.TryGetValue((componente, systemId), out var existente) && !existente.HasExited) ||
                await EstaOnlineAsync(componente, systemId))
            {
                LogsProcesos.Add(componente, systemId, "info", $"Inicio solicitado: {nombre} ya en ejecucion.");
                return new(true, "running", $"El {nombre} ya esta en ejecucion.");
            }

            var puerto = Puerto(componente, systemId);
            if (PuertosSistemas.Ocupado(puerto))
            {
                var variable = componente == Componente.Backend ? "PUERTO_BASE_BACKEND" : "PUERTO_BASE_FRONTEND";
                return new(false, "error", $"El puerto {puerto} está ocupado por otro programa. Liberalo o cambiá {variable} en el .env de la fábrica.");
            }

            var log = LogsProcesos.Reset(componente, systemId);
            var preparacion = componente == Componente.Backend ? PrepararBackend(carpeta, log) : PrepararFrontend(carpeta, log);
            if (preparacion != null)
                return new(false, "error", preparacion);

            var startInfo = componente == Componente.Backend
                ? new ProcessStartInfo("dotnet", "watch run")
                : new ProcessStartInfo("npm", $"run dev -- --port {puerto} --strictPort"); // --strictPort: no saltar a otro puerto en silencio
            startInfo.WorkingDirectory = carpeta;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            if (componente == Componente.Backend)
            {
                startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
                startInfo.Environment["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"] = "1";
            }
            else
            {
                startInfo.Environment["BROWSER"] = "none";
                startInfo.Environment["VITE_PORT"] = puerto.ToString();
            }

            log.Add("info", $"Iniciando {nombre} ({startInfo.FileName} {startInfo.Arguments})...");
            var proceso = Process.Start(startInfo);
            if (proceso == null)
            {
                log.Add("error", $"No se pudo iniciar el proceso {startInfo.FileName}.");
                return new(false, "error", $"No se pudo iniciar el {nombre}.");
            }

            proceso.EnableRaisingEvents = true;
            proceso.OutputDataReceived += (_, args) => log.Add("stdout", args.Data ?? string.Empty);
            proceso.ErrorDataReceived += (_, args) => log.Add("stderr", args.Data ?? string.Empty);
            proceso.BeginOutputReadLine();
            proceso.BeginErrorReadLine();
            proceso.Exited += (_, _) =>
            {
                log.Add("info", $"{startInfo.FileName} terminó (exit code {proceso.ExitCode}).");
                Procesos.TryRemove((componente, systemId), out _);
            };

            Procesos[(componente, systemId)] = proceso;
            return new(true, "started", componente == Componente.Backend ? "Backend iniciando..." : "Frontend iniciando...",
                componente == Componente.Frontend ? puerto : null);
        }

        public static ResultadoProceso Detener(Componente componente, int systemId)
        {
            var nombre = Nombre(componente);
            if (!Procesos.TryGetValue((componente, systemId), out var proceso) || proceso.HasExited)
            {
                Procesos.TryRemove((componente, systemId), out _);
                LogsProcesos.Add(componente, systemId, "info", $"Detener solicitado: {nombre} no estaba en ejecucion.");
                return new(true, "not_running", $"El {nombre} no esta en ejecucion.");
            }

            try
            {
                LogsProcesos.Add(componente, systemId, "info", $"Deteniendo {nombre}...");
                proceso.Kill(true);
                proceso.WaitForExit(10000);
            }
            catch
            {
                // Ya terminó.
            }

            Procesos.TryRemove((componente, systemId), out _);
            LogsProcesos.Add(componente, systemId, "info", componente == Componente.Backend ? "Backend detenido." : "Frontend detenido.");
            return new(true, "stopped", componente == Componente.Backend ? "Backend detenido." : "Frontend detenido.");
        }

        public static async Task<bool> EstaOnlineAsync(Componente componente, int systemId)
        {
            try
            {
                var url = componente == Componente.Backend
                    ? $"http://localhost:{Puerto(componente, systemId)}/{ApiBase(systemId)}/dev/ping"
                    : $"http://localhost:{Puerto(componente, systemId)}";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var response = await Http.GetAsync(url, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Corre un comando y espera a que termine (con límite de tiempo).</summary>
        public static (bool Ok, string Output, string Error) EjecutarComando(string archivo, string argumentos, string carpeta, TimeSpan limite)
        {
            try
            {
                var startInfo = new ProcessStartInfo(archivo, argumentos)
                {
                    WorkingDirectory = carpeta,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proceso = Process.Start(startInfo);
                if (proceso == null)
                    return (false, string.Empty, $"No se pudo iniciar {archivo} {argumentos}.");

                var salida = proceso.StandardOutput.ReadToEndAsync();
                var error = proceso.StandardError.ReadToEndAsync();
                if (!proceso.WaitForExit((int)limite.TotalMilliseconds))
                {
                    try { proceso.Kill(true); } catch { /* ya terminó */ }
                    return (false, salida.IsCompleted ? salida.Result : string.Empty, $"Timeout ejecutando {archivo} {argumentos}.");
                }

                return (proceso.ExitCode == 0, salida.Result, error.Result);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        public static (bool Ok, string Output, string Error) DotnetRestore(string carpeta) =>
            EjecutarComando("dotnet", "restore", carpeta, TimeSpan.FromMinutes(2));

        private static string? PrepararBackend(string carpeta, LogBuffer log)
        {
            var restore = DotnetRestore(carpeta);
            if (!restore.Ok)
            {
                log.Add("error", $"dotnet restore fallo: {restore.Error}");
                return "dotnet restore fallo. Revisa la consola del backend.";
            }
            log.Add("info", "dotnet restore ok.");
            return null;
        }

        private static string? PrepararFrontend(string carpeta, LogBuffer log)
        {
            var nodeModules = Path.Combine(carpeta, "node_modules");
            if (!NecesitaNpmInstall(carpeta, nodeModules))
                return null;

            log.Add("info", Directory.Exists(nodeModules)
                ? "Dependencias incompletas detectadas. Reinstalando (npm install)..."
                : "Instalando dependencias (npm install)...");

            var install = EjecutarComando("npm", "install", carpeta, TimeSpan.FromMinutes(4));
            if (!install.Ok)
            {
                log.Add("error", $"npm install fallo: {install.Error}");
                return "npm install fallo. Revisa la consola del frontend.";
            }
            log.Add("info", "npm install ok.");
            return null;
        }

        private static bool NecesitaNpmInstall(string carpeta, string nodeModules)
        {
            var bin = Path.Combine(nodeModules, ".bin");
            var tieneVite = new[] { "vite", "vite.cmd", "vite.ps1" }.Any(f => File.Exists(Path.Combine(bin, f)));
            return !tieneVite || !File.Exists(Path.Combine(carpeta, "package.json"));
        }

        private static string ApiBase(int systemId)
        {
            var apiBase = BackendConfigGestor.ObtenerPorSistema(systemId)?.System?.ApiBase;
            return (string.IsNullOrWhiteSpace(apiBase) ? "api/v1" : apiBase.Trim()).Trim('/');
        }
    }
}
