using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Backend.Models.Sistemas;
using Backend.Negocio.Gestores;

namespace Backend.Negocio.Generadores
{
    public static class SistemasFrontendGenerator
    {
        public static FrontendGenerateResult Generar(int systemId, string frontendSource, string outputRoot, bool overwrite)
        {
            var system = SistemasGestor.ObtenerPorId(systemId);
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

        private static void UpdateAxiosBaseUrl(string frontendPath, int systemId)
        {
            var config = BackendConfigGestor.ObtenerPorSistema(systemId);
            var apiBase = config.System?.ApiBase ?? "api/v1";
            apiBase = apiBase.Trim('/');
            if (string.IsNullOrWhiteSpace(apiBase))
                apiBase = "api/v1";

            var port = 5032 + systemId;
            var baseUrl = $"http://localhost:{port}/{apiBase}";

            var axiosPath = Path.Combine(frontendPath, "src", "api", "axios.js");
            if (!File.Exists(axiosPath))
                return;

            var content = File.ReadAllText(axiosPath, Encoding.UTF8);
            var updated = Regex.Replace(content, @"baseURL:\s*['""][^'""]*['""]", $"baseURL: '{baseUrl}'");
            File.WriteAllText(axiosPath, updated, new UTF8Encoding(false));
        }

        private static void WriteFrontendConfig(string frontendPath, int systemId)
        {
            var config = FrontendConfigGestor.ObtenerPorSistema(systemId);
            var configDir = Path.Combine(frontendPath, "src", "config");
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

        private static void CopyDirectory(string sourceDir, string targetDir, HashSet<string> excludedDirectories, HashSet<string> excludedFiles)
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

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
