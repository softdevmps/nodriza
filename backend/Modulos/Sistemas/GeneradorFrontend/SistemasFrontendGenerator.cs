using Backend.Comun;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Backend.Modulos.Sistemas.GeneradorBackend;
using Backend.Modulos.Sistemas.GeneradorFrontend.Modelos;

namespace Backend.Modulos.Sistemas.GeneradorFrontend
{
    public class SistemasFrontendGenerator
    {
        private readonly BackendConfigGestor _backendConfigGestor;
        private readonly FrontendConfigGestor _frontendConfigGestor;
        private readonly SistemasGestor _sistemasGestor;

        public SistemasFrontendGenerator(BackendConfigGestor backendConfigGestor, FrontendConfigGestor frontendConfigGestor, SistemasGestor sistemasGestor)
        {
            _backendConfigGestor = backendConfigGestor;
            _frontendConfigGestor = frontendConfigGestor;
            _sistemasGestor = sistemasGestor;
        }

        public FrontendGenerateResult Generar(int systemId, string frontendSource, string outputRoot, bool overwrite)
        {
            var system = _sistemasGestor.ObtenerPorId(systemId);
            if (system == null)
            {
                return new FrontendGenerateResult
                {
                    Ok = false,
                    Message = "Sistema no encontrado."
                };
            }

            if (!Directory.Exists(frontendSource))
            {
                return new FrontendGenerateResult
                {
                    Ok = false,
                    Message = "No se encontro la carpeta frontend-runtime para generar."
                };
            }

            if (Directory.Exists(outputRoot))
            {
                if (!overwrite)
                {
                    return new FrontendGenerateResult
                    {
                        Ok = false,
                        Message = $"La carpeta ya existe: {outputRoot}. Usa overwrite=true para reemplazar."
                    };
                }

                Directory.Delete(outputRoot, true);
            }

            Directory.CreateDirectory(outputRoot);

            CopyDirectory(
                frontendSource,
                outputRoot,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "node_modules", "dist", ".vscode" },
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".env", ".ds_store" }
            );

            UpdateAxiosBaseUrl(outputRoot, systemId);
            WriteFrontendConfig(outputRoot, systemId);

            return new FrontendGenerateResult
            {
                Ok = true,
                Message = "Frontend generado correctamente.",
                OutputPath = outputRoot
            };
        }

        private void UpdateAxiosBaseUrl(string frontendPath, int systemId)
        {
            var config = _backendConfigGestor.ObtenerPorSistema(systemId);
            var apiBase = config.System?.ApiBase ?? "api/v1";
            apiBase = apiBase.Trim('/');
            if (string.IsNullOrWhiteSpace(apiBase))
                apiBase = "api/v1";

            var port = PuertosSistemas.Backend(systemId);
            var baseUrl = $"http://localhost:{port}/{apiBase}";

            var axiosPath = Path.Combine(frontendPath, "src", "comun", "api", "axios.js");
            if (!File.Exists(axiosPath))
                return;

            var content = File.ReadAllText(axiosPath, Encoding.UTF8);
            var updated = Regex.Replace(content, @"baseURL:\s*['""][^'""]*['""]", $"baseURL: '{baseUrl}'");
            File.WriteAllText(axiosPath, updated, new UTF8Encoding(false));
        }

        private void WriteFrontendConfig(string frontendPath, int systemId)
        {
            var config = _frontendConfigGestor.ObtenerPorSistema(systemId);
            var configDir = Path.Combine(frontendPath, "src", "comun", "config");
            Directory.CreateDirectory(configDir);

            var json = JsonSerializer.Serialize(
                config,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            var path = Path.Combine(configDir, "frontend-config.json");
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private void CopyDirectory(string sourceDir, string targetDir, HashSet<string> excludedDirectories, HashSet<string> excludedFiles)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                if (excludedFiles.Contains(fileName))
                    continue;

                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                if (excludedDirectories.Contains(dirName))
                    continue;

                var destDir = Path.Combine(targetDir, dirName);
                CopyDirectory(dir, destDir, excludedDirectories, excludedFiles);
            }
        }

        private void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
