using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Comun;
using Backend.Comun.Seguridad;

namespace Backend.Modulos.Sistemas.Exportacion
{
    [ApiController]
    [Authorize(Policy = Politicas.Admin)]
    public class ExportacionController : AppController
    {
        private readonly SistemasExportador _sistemasExportador;
        private readonly SistemasGestor _sistemasGestor;
        private readonly IWebHostEnvironment _env;

        public ExportacionController(IWebHostEnvironment env, SistemasExportador sistemasExportador, SistemasGestor sistemasGestor)
        {
            _sistemasExportador = sistemasExportador;
            _sistemasGestor = sistemasGestor;
            _env = env;
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
                var sistema = _sistemasGestor.ObtenerPorId(id);
                if (sistema == null)
                    return NotFound();

                var workspacePath = Path.Combine(systemsRoot, sistema.Slug);
                if (!Directory.Exists(workspacePath))
                {
                    var exportResult = _sistemasExportador.Exportar(
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

                var metadataResult = _sistemasExportador.ActualizarMetadata(
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

                // Un workspace puede tener solo el backend (o solo el frontend) generado:
                // lo que falte se completa con las mismas fuentes que usa el export completo.
                var complementos = new List<(string Origen, string Prefijo)>();
                if (!Directory.Exists(Path.Combine(workspacePath, "backend")))
                    complementos.Add((_env.ContentRootPath, "backend"));
                if (!Directory.Exists(Path.Combine(workspacePath, "frontend")))
                    complementos.Add((Path.Combine(repoRoot, "frontend-runtime"), "frontend"));

                SistemasExportador.CrearZipSeguro(zipPath, workspacePath, complementos);
                return PhysicalFile(zipPath, "application/zip", zipFileName);
            }

            var exportRoot = normalizedMode == "workspace" ? systemsRoot : exportsRoot;

            var result = _sistemasExportador.Exportar(
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
    }
}
