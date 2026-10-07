using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class SistemasExportador
    {
        private static readonly string[] Actions = { "view", "create", "edit", "delete" };

        public static ExportResult ActualizarMetadata(
            int systemId,
            string exportPath,
            string contentRootPath,
            bool includeAdminMenus)
        {
            using var context = new SystemBaseContext();

            var system = context.Systems
                .Include(s => s.Entities)
                    .ThenInclude(e => e.Fields)
                .Include(s => s.Relations)
                .FirstOrDefault(s => s.Id == systemId);

            if (system == null)
            {
                return new ExportResult
                {
                    Ok = false,
                    Message = "Sistema no encontrado."
                };
            }

            if (system.Entities.Count == 0)
            {
                return new ExportResult
                {
                    Ok = false,
                    Message = "El sistema no tiene entidades."
                };
            }

            var schemaName = ToSafeSchemaName(system.Slug);
            if (schemaName == null)
            {
                return new ExportResult
                {
                    Ok = false,
                    Message = "Slug invalido para crear schema."
                };
            }

            foreach (var entity in system.Entities)
            {
                if (entity.Fields.Count == 0)
                {
                    return new ExportResult
                    {
                        Ok = false,
                        Message = $"Entidad sin campos: {entity.Name}"
                    };
                }

                if (ToSafeSqlName(entity.TableName) == null)
                {
                    return new ExportResult
                    {
                        Ok = false,
                        Message = $"TableName invalido: {entity.TableName}"
                    };
                }

                foreach (var field in entity.Fields)
                {
                    if (ToSafeSqlName(field.ColumnName) == null)
                    {
                        return new ExportResult
                        {
                            Ok = false,
                            Message = $"ColumnName invalido: {field.ColumnName}"
                        };
                    }
                }
            }

            try
            {
                Directory.CreateDirectory(exportPath);

                var metadataSql = ReadMetadataSql(contentRootPath);
                var frontendConfig = FrontendConfigGestor.ObtenerPorSistema(system.Id);
                var adminUser = string.IsNullOrWhiteSpace(frontendConfig?.System?.SeedAdminUser) ? "admin" : frontendConfig.System.SeedAdminUser;
                var adminPassword = string.IsNullOrWhiteSpace(frontendConfig?.System?.SeedAdminPassword) ? "admin" : frontendConfig.System.SeedAdminPassword;
                var adminEmail = string.IsNullOrWhiteSpace(frontendConfig?.System?.SeedAdminEmail)
                    ? $"{adminUser}@local"
                    : frontendConfig.System.SeedAdminEmail;
                var adminHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

                var dbName = ToSafeSqlName(Environment.GetEnvironmentVariable("DB_NAME")) ?? "systemBase";
                var databaseSql = BuildDatabaseScript(
                    system,
                    schemaName,
                    metadataSql,
                    adminUser,
                    adminEmail,
                    adminHash,
                    includeAdminMenus,
                    dbName
                );

                var databasePath = Path.Combine(exportPath, "database.sql");
                File.WriteAllText(databasePath, databaseSql, new UTF8Encoding(false));

                var manifest = BuildManifest(system, schemaName);
                var manifestPath = Path.Combine(exportPath, "manifest.json");
                var manifestJson = JsonSerializer.Serialize(
                    manifest,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                File.WriteAllText(manifestPath, manifestJson, new UTF8Encoding(false));

                var readmePath = Path.Combine(exportPath, "README.md");
                File.WriteAllText(readmePath, BuildReadme(system, schemaName, includeAdminMenus, adminUser, adminPassword, dbName), new UTF8Encoding(false));

                return new ExportResult
                {
                    Ok = true,
                    Message = "Metadata actualizada correctamente.",
                    ExportPath = exportPath,
                    Files = new List<string> { "manifest.json", "database.sql", "README.md" }
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Ok = false,
                    Message = $"Error al actualizar metadata: {ex.Message}"
                };
            }
        }

        public static ExportResult Exportar(
            int systemId,
            string exportRoot,
            string contentRootPath,
            bool includeAdminMenus,
            bool workspaceMode,
            bool overwrite)
        {
            using var context = new SystemBaseContext();

            var system = context.Systems
                .Include(s => s.Entities)
                    .ThenInclude(e => e.Fields)
                .Include(s => s.Relations)
                .FirstOrDefault(s => s.Id == systemId);

            if (system == null)
            {
                return new ExportResult
                {
                    Ok = false,
                    Message = "Sistema no encontrado."
                };
            }

            if (system.Entities.Count == 0)
            {
                return new ExportResult
                {
                    Ok = false,
                    Message = "El sistema no tiene entidades."
                };
            }

            var schemaName = ToSafeSchemaName(system.Slug);
            if (schemaName == null)
            {
                return new ExportResult
                {
                    Ok = false,
                    Message = "Slug invalido para crear schema."
                };
            }

            foreach (var entity in system.Entities)
            {
                if (entity.Fields.Count == 0)
                {
                    return new ExportResult
                    {
                        Ok = false,
                        Message = $"Entidad sin campos: {entity.Name}"
                    };
                }

                if (ToSafeSqlName(entity.TableName) == null)
                {
                    return new ExportResult
                    {
                        Ok = false,
                        Message = $"TableName invalido: {entity.TableName}"
                    };
                }

                foreach (var field in entity.Fields)
                {
                    if (ToSafeSqlName(field.ColumnName) == null)
                    {
                        return new ExportResult
                        {
                            Ok = false,
                            Message = $"ColumnName invalido: {field.ColumnName}"
                        };
                    }
                }
            }

            try
            {
                Directory.CreateDirectory(exportRoot);

                var exportFolderName = workspaceMode ? ToSafeFolderSegment(system.Slug) ?? "system" : BuildExportFolderName(system);
                var exportPath = Path.Combine(exportRoot, exportFolderName);

                if (workspaceMode && Directory.Exists(exportPath))
                {
                    if (!overwrite)
                    {
                        return new ExportResult
                        {
                            Ok = false,
                            Message = $"La carpeta ya existe: {exportPath}. Usa overwrite=true para reemplazar."
                        };
                    }

                    Directory.Delete(exportPath, true);
                }

                Directory.CreateDirectory(exportPath);

                var metadataSql = ReadMetadataSql(contentRootPath);
                var frontendConfig = FrontendConfigGestor.ObtenerPorSistema(system.Id);
                var adminUser = string.IsNullOrWhiteSpace(frontendConfig?.System?.SeedAdminUser) ? "admin" : frontendConfig.System.SeedAdminUser;
                var adminPassword = string.IsNullOrWhiteSpace(frontendConfig?.System?.SeedAdminPassword) ? "admin" : frontendConfig.System.SeedAdminPassword;
                var adminEmail = string.IsNullOrWhiteSpace(frontendConfig?.System?.SeedAdminEmail)
                    ? $"{adminUser}@local"
                    : frontendConfig.System.SeedAdminEmail;
                var adminHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

                var dbName = ToSafeSqlName(Environment.GetEnvironmentVariable("DB_NAME")) ?? "systemBase";
                var databaseSql = BuildDatabaseScript(
                    system,
                    schemaName,
                    metadataSql,
                    adminUser,
                    adminEmail,
                    adminHash,
                    includeAdminMenus,
                    dbName
                );
                var databasePath = Path.Combine(exportPath, "database.sql");
                File.WriteAllText(databasePath, databaseSql, new UTF8Encoding(false));

                var manifest = BuildManifest(system, schemaName);
                var manifestPath = Path.Combine(exportPath, "manifest.json");
                var manifestJson = JsonSerializer.Serialize(
                    manifest,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                File.WriteAllText(manifestPath, manifestJson, new UTF8Encoding(false));

                var readmePath = Path.Combine(exportPath, "README.md");
                File.WriteAllText(readmePath, BuildReadme(system, schemaName, includeAdminMenus, adminUser, adminPassword, dbName), new UTF8Encoding(false));

                var repoRoot = Directory.GetParent(contentRootPath)?.FullName;
                if (string.IsNullOrWhiteSpace(repoRoot))
                {
                    return new ExportResult
                    {
                        Ok = false,
                        Message = "No se pudo resolver la raiz del repositorio."
                    };
                }

                var backendSource = contentRootPath;
                var frontendSource = Path.Combine(repoRoot, "frontend-runtime");
                if (!Directory.Exists(backendSource) || !Directory.Exists(frontendSource))
                {
                    return new ExportResult
                    {
                        Ok = false,
                        Message = "No se encontraron carpetas backend/frontend-runtime para exportar."
                    };
                }

                var backendDest = Path.Combine(exportPath, "backend");
                var frontendDest = Path.Combine(exportPath, "frontend");

                CopyDirectory(
                    backendSource,
                    backendDest,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "exports" },
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".env", ".ds_store" }
                );

                // Make env example visible in Finder (macOS hides dotfiles)
                var hiddenEnvExample = Path.Combine(backendDest, ".env.example");
                if (File.Exists(hiddenEnvExample))
                {
                    var visibleEnvExample = Path.Combine(backendDest, "env.example");
                    File.Copy(hiddenEnvExample, visibleEnvExample, true);
                }

                CopyDirectory(
                    frontendSource,
                    frontendDest,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "node_modules", "dist", ".vscode" },
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".env", ".ds_store" }
                );

                if (!includeAdminMenus)
                {
                    // runtime-only template already stripped
                }

                string? zipFileName = null;
                string? zipPath = null;

                if (!workspaceMode)
                {
                    zipFileName = $"{exportFolderName}.zip";
                    zipPath = Path.Combine(exportRoot, zipFileName);
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);

                    ZipFile.CreateFromDirectory(exportPath, zipPath, CompressionLevel.Fastest, false);
                }

                return new ExportResult
                {
                    Ok = true,
                    Message = "Export generado correctamente.",
                    ExportPath = exportPath,
                    ZipPath = zipPath,
                    ZipFileName = zipFileName,
                    Files = new List<string>
                    {
                        "manifest.json",
                        "database.sql",
                        "README.md",
                        "backend/",
                        "frontend/"
                    }
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Ok = false,
                    Message = $"Error al exportar: {ex.Message}"
                };
            }
        }

        private static string BuildDatabaseScript(
            Systems system,
            string schemaName,
            string? metadataSql,
            string adminUser,
            string adminEmail,
            string adminHash,
            bool includeAdminMenus,
            string databaseName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("-- =============================");
            sb.AppendLine("-- SystemBase Export - Database");
            sb.AppendLine("-- =============================");
            sb.AppendLine();
            sb.AppendLine($"IF DB_ID(N'{EscapeSql(databaseName)}') IS NULL");
            sb.AppendLine("BEGIN");
            sb.AppendLine($"    CREATE DATABASE [{databaseName}];");
            sb.AppendLine("END");
            sb.AppendLine("GO");
            sb.AppendLine($"USE [{databaseName}];");
            sb.AppendLine("GO");
            sb.AppendLine();
            sb.AppendLine("-- Base tables (dbo)");
            sb.AppendLine(BuildBaseTablesScript());
            sb.AppendLine();
            sb.AppendLine("-- Metadata schema (sb)");
            if (!string.IsNullOrWhiteSpace(metadataSql))
                sb.AppendLine(metadataSql);
            else
                sb.AppendLine("-- WARNING: metadata SQL no encontrado.");

            sb.AppendLine();
            sb.AppendLine("-- Seed base data (admin + menus)");
            sb.AppendLine(BuildBaseSeedScript(adminUser, adminEmail, adminHash, includeAdminMenus));
            sb.AppendLine();
            sb.AppendLine("-- Seed system metadata");
            sb.AppendLine(BuildSystemMetadataInserts(system));
            sb.AppendLine();
            sb.AppendLine("-- Runtime schema");
            sb.AppendLine(BuildRuntimeSchemaScript(schemaName, system.Entities, system.Relations));

            return sb.ToString();
        }

        private static string BuildBaseTablesScript()
        {
            var sb = new StringBuilder();

            sb.AppendLine("IF OBJECT_ID('dbo.Roles', 'U') IS NULL");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    CREATE TABLE dbo.Roles (");
            sb.AppendLine("        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,");
            sb.AppendLine("        Nombre VARCHAR(50) NOT NULL,");
            sb.AppendLine("        Activo BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1)");
            sb.AppendLine("    );");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    CREATE TABLE dbo.Usuarios (");
            sb.AppendLine("        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,");
            sb.AppendLine("        Username VARCHAR(50) NOT NULL,");
            sb.AppendLine("        Email VARCHAR(100) NOT NULL,");
            sb.AppendLine("        PasswordHash VARCHAR(255) NOT NULL,");
            sb.AppendLine("        Nombre VARCHAR(100) NOT NULL,");
            sb.AppendLine("        Apellido VARCHAR(100) NOT NULL,");
            sb.AppendLine("        Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),");
            sb.AppendLine("        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT (GETDATE()),");
            sb.AppendLine("        RolId INT NULL,");
            sb.AppendLine("        CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles (Id)");
            sb.AppendLine("    );");
            sb.AppendLine("    CREATE UNIQUE INDEX UX_Usuarios_Username ON dbo.Usuarios (Username);");
            sb.AppendLine("    CREATE UNIQUE INDEX UX_Usuarios_Email ON dbo.Usuarios (Email);");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("IF OBJECT_ID('dbo.Menus', 'U') IS NULL");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    CREATE TABLE dbo.Menus (");
            sb.AppendLine("        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Menus PRIMARY KEY,");
            sb.AppendLine("        Titulo VARCHAR(100) NOT NULL,");
            sb.AppendLine("        Icono VARCHAR(50) NOT NULL,");
            sb.AppendLine("        Ruta VARCHAR(100) NULL,");
            sb.AppendLine("        Orden INT NOT NULL,");
            sb.AppendLine("        PadreId INT NULL,");
            sb.AppendLine("        Activo BIT NOT NULL CONSTRAINT DF_Menus_Activo DEFAULT (1),");
            sb.AppendLine("        CONSTRAINT FK_Menus_Padre FOREIGN KEY (PadreId) REFERENCES dbo.Menus (Id)");
            sb.AppendLine("    );");
            sb.AppendLine("    CREATE INDEX IX_Menus_Activo ON dbo.Menus (Activo);");
            sb.AppendLine("    CREATE INDEX IX_Menus_Orden ON dbo.Menus (Orden);");
            sb.AppendLine("    CREATE INDEX IX_Menus_PadreId ON dbo.Menus (PadreId);");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("IF OBJECT_ID('dbo.RolMenu', 'U') IS NULL");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    CREATE TABLE dbo.RolMenu (");
            sb.AppendLine("        RolId INT NOT NULL,");
            sb.AppendLine("        MenuId INT NOT NULL,");
            sb.AppendLine("        CONSTRAINT PK_RolMenu PRIMARY KEY (RolId, MenuId),");
            sb.AppendLine("        CONSTRAINT FK_RolMenu_Rol FOREIGN KEY (RolId) REFERENCES dbo.Roles (Id),");
            sb.AppendLine("        CONSTRAINT FK_RolMenu_Menu FOREIGN KEY (MenuId) REFERENCES dbo.Menus (Id)");
            sb.AppendLine("    );");
            sb.AppendLine("END");

            return sb.ToString();
        }

        private static string BuildBaseSeedScript(string adminUser, string adminEmail, string adminHash, bool includeAdminMenus)
        {
            var sb = new StringBuilder();

            sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Admin')");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO dbo.Roles (Nombre, Activo)");
            sb.AppendLine("    VALUES ('Admin', 1);");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Username = '{EscapeSql(adminUser)}')");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO dbo.Usuarios (Username, Email, PasswordHash, Nombre, Apellido, Activo, FechaCreacion, RolId)");
            sb.AppendLine($"    SELECT '{EscapeSql(adminUser)}', '{EscapeSql(adminEmail)}',");
            sb.AppendLine($"           '{EscapeSql(adminHash)}',");
            sb.AppendLine("           'Admin', 'System', 1, GETDATE(), r.Id");
            sb.AppendLine("    FROM dbo.Roles r");
            sb.AppendLine("    WHERE r.Nombre = 'Admin';");
            sb.AppendLine("END");
            sb.AppendLine();

            if (!includeAdminMenus)
                return sb.ToString();

            sb.AppendLine("-- Menus base (full export)");
            sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Titulo = 'Home' AND PadreId IS NULL)");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO dbo.Menus (Titulo, Icono, Ruta, Orden, PadreId, Activo)");
            sb.AppendLine("    VALUES ('Home', 'mdi-home', '/home', 1, NULL, 1);");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL)");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO dbo.Menus (Titulo, Icono, Ruta, Orden, PadreId, Activo)");
            sb.AppendLine("    VALUES ('Sistema', 'mdi-cog', NULL, 2, NULL, 1);");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Titulo = 'Sistemas' AND PadreId = (SELECT TOP 1 Id FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL))");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO dbo.Menus (Titulo, Icono, Ruta, Orden, PadreId, Activo)");
            sb.AppendLine("    SELECT 'Sistemas', 'mdi-apps', '/sistemas', 1, Id, 1");
            sb.AppendLine("    FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL;");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Titulo = 'Menu' AND PadreId = (SELECT TOP 1 Id FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL))");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO dbo.Menus (Titulo, Icono, Ruta, Orden, PadreId, Activo)");
            sb.AppendLine("    SELECT 'Menu', 'mdi-view-list', '/menu', 2, Id, 1");
            sb.AppendLine("    FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL;");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Titulo = 'Usuarios' AND PadreId = (SELECT TOP 1 Id FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL))");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO dbo.Menus (Titulo, Icono, Ruta, Orden, PadreId, Activo)");
            sb.AppendLine("    SELECT 'Usuarios', 'mdi-account-group', '/usuarios', 3, Id, 1");
            sb.AppendLine("    FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL;");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Titulo = 'Roles' AND PadreId = (SELECT TOP 1 Id FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL))");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO dbo.Menus (Titulo, Icono, Ruta, Orden, PadreId, Activo)");
            sb.AppendLine("    SELECT 'Roles', 'mdi-shield-account', '/roles', 4, Id, 1");
            sb.AppendLine("    FROM dbo.Menus WHERE Titulo = 'Sistema' AND PadreId IS NULL;");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("-- Asignar menus base al rol Admin");
            sb.AppendLine("INSERT INTO dbo.RolMenu (RolId, MenuId)");
            sb.AppendLine("SELECT r.Id, m.Id");
            sb.AppendLine("FROM dbo.Roles r");
            sb.AppendLine("JOIN dbo.Menus m ON m.Titulo IN ('Home','Sistema','Sistemas','Menu','Usuarios','Roles')");
            sb.AppendLine("WHERE r.Nombre = 'Admin'");
            sb.AppendLine("AND NOT EXISTS (");
            sb.AppendLine("    SELECT 1 FROM dbo.RolMenu rm");
            sb.AppendLine("    WHERE rm.RolId = r.Id AND rm.MenuId = m.Id");
            sb.AppendLine(");");

            return sb.ToString();
        }

        private static string BuildSystemMetadataInserts(Systems system)
        {
            var sb = new StringBuilder();
            var slug = EscapeSql(system.Slug);

            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sb.Systems WHERE Slug = N'{slug}')");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    INSERT INTO sb.Systems (Slug, Name, Namespace, Description, Status, Version, IsActive, PublishedAt)");
            sb.AppendLine("    VALUES (");
            sb.AppendLine($"        N'{slug}',");
            sb.AppendLine($"        {SqlValue(system.Name)},");
            sb.AppendLine($"        {SqlValue(system.Namespace)},");
            sb.AppendLine($"        {SqlValue(system.Description)},");
            sb.AppendLine($"        {SqlValue(system.Status)},");
            sb.AppendLine($"        {SqlValue(system.Version)},");
            sb.AppendLine($"        {(system.IsActive ? 1 : 0)},");
            sb.AppendLine($"        {SqlDate(system.PublishedAt)}");
            sb.AppendLine("    );");
            sb.AppendLine("END");
            sb.AppendLine();

            foreach (var entity in system.Entities.OrderBy(e => e.SortOrder).ThenBy(e => e.Id))
            {
                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sb.Entities e WHERE e.SystemId = (SELECT Id FROM sb.Systems WHERE Slug = N'{slug}') AND e.Name = N'{EscapeSql(entity.Name)}')");
                sb.AppendLine("BEGIN");
                sb.AppendLine("    INSERT INTO sb.Entities (SystemId, Name, TableName, DisplayName, Description, IsActive, SortOrder)");
                sb.AppendLine("    SELECT s.Id,");
                sb.AppendLine($"        N'{EscapeSql(entity.Name)}',");
                sb.AppendLine($"        N'{EscapeSql(entity.TableName)}',");
                sb.AppendLine($"        {SqlValue(entity.DisplayName)},");
                sb.AppendLine($"        {SqlValue(entity.Description)},");
                sb.AppendLine($"        {(entity.IsActive ? 1 : 0)},");
                sb.AppendLine($"        {entity.SortOrder}");
                sb.AppendLine("    FROM sb.Systems s WHERE s.Slug = N'" + slug + "';");
                sb.AppendLine("END");
                sb.AppendLine();

                foreach (var field in entity.Fields.OrderBy(f => f.SortOrder).ThenBy(f => f.Id))
                {
                    sb.AppendLine("IF NOT EXISTS (");
                    sb.AppendLine("    SELECT 1 FROM sb.Fields f");
                    sb.AppendLine("    JOIN sb.Entities e ON e.Id = f.EntityId");
                    sb.AppendLine("    JOIN sb.Systems s ON s.Id = e.SystemId");
                    sb.AppendLine($"    WHERE s.Slug = N'{slug}' AND e.Name = N'{EscapeSql(entity.Name)}' AND f.Name = N'{EscapeSql(field.Name)}'");
                    sb.AppendLine(")");
                    sb.AppendLine("BEGIN");
                    sb.AppendLine("    INSERT INTO sb.Fields (EntityId, Name, ColumnName, DataType, Required, MaxLength, Precision, Scale, DefaultValue, IsPrimaryKey, IsIdentity, IsUnique, UiConfigJson, SortOrder)");
                    sb.AppendLine("    SELECT e.Id,");
                    sb.AppendLine($"        N'{EscapeSql(field.Name)}',");
                    sb.AppendLine($"        N'{EscapeSql(field.ColumnName)}',");
                    sb.AppendLine($"        N'{EscapeSql(field.DataType)}',");
                    sb.AppendLine($"        {(field.Required ? 1 : 0)},");
                    sb.AppendLine($"        {SqlNumber(field.MaxLength)},");
                    sb.AppendLine($"        {SqlNumber(field.Precision)},");
                    sb.AppendLine($"        {SqlNumber(field.Scale)},");
                    sb.AppendLine($"        {SqlValue(field.DefaultValue)},");
                    sb.AppendLine($"        {(field.IsPrimaryKey ? 1 : 0)},");
                    sb.AppendLine($"        {(field.IsIdentity ? 1 : 0)},");
                    sb.AppendLine($"        {(field.IsUnique ? 1 : 0)},");
                    sb.AppendLine($"        {SqlValue(field.UiConfigJson)},");
                    sb.AppendLine($"        {field.SortOrder}");
                    sb.AppendLine("    FROM sb.Entities e");
                    sb.AppendLine("    JOIN sb.Systems s ON s.Id = e.SystemId");
                    sb.AppendLine($"    WHERE s.Slug = N'{slug}' AND e.Name = N'{EscapeSql(entity.Name)}';");
                    sb.AppendLine("END");
                    sb.AppendLine();
                }
            }

            foreach (var relation in system.Relations.OrderBy(r => r.Id))
            {
                var sourceName = system.Entities.FirstOrDefault(e => e.Id == relation.SourceEntityId)?.Name;
                var targetName = system.Entities.FirstOrDefault(e => e.Id == relation.TargetEntityId)?.Name;
                if (sourceName == null || targetName == null)
                    continue;

                sb.AppendLine("IF NOT EXISTS (");
                sb.AppendLine("    SELECT 1 FROM sb.Relations r");
                sb.AppendLine("    JOIN sb.Systems s ON s.Id = r.SystemId");
                sb.AppendLine("    JOIN sb.Entities src ON src.Id = r.SourceEntityId");
                sb.AppendLine("    JOIN sb.Entities tgt ON tgt.Id = r.TargetEntityId");
                sb.AppendLine($"    WHERE s.Slug = N'{slug}'");
                sb.AppendLine($"      AND src.Name = N'{EscapeSql(sourceName)}'");
                sb.AppendLine($"      AND tgt.Name = N'{EscapeSql(targetName)}'");
                sb.AppendLine($"      AND ISNULL(r.ForeignKey, '') = ISNULL(N'{EscapeSql(relation.ForeignKey)}', '')");
                sb.AppendLine(")");
                sb.AppendLine("BEGIN");
                sb.AppendLine("    INSERT INTO sb.Relations (SystemId, SourceEntityId, TargetEntityId, RelationType, ForeignKey, InverseProperty, CascadeDelete)");
                sb.AppendLine("    SELECT s.Id, src.Id, tgt.Id,");
                sb.AppendLine($"        N'{EscapeSql(relation.RelationType)}',");
                sb.AppendLine($"        {SqlValue(relation.ForeignKey)},");
                sb.AppendLine($"        {SqlValue(relation.InverseProperty)},");
                sb.AppendLine($"        {(relation.CascadeDelete ? 1 : 0)}");
                sb.AppendLine("    FROM sb.Systems s");
                sb.AppendLine("    JOIN sb.Entities src ON src.SystemId = s.Id");
                sb.AppendLine("    JOIN sb.Entities tgt ON tgt.SystemId = s.Id");
                sb.AppendLine($"    WHERE s.Slug = N'{slug}'");
                sb.AppendLine($"      AND src.Name = N'{EscapeSql(sourceName)}'");
                sb.AppendLine($"      AND tgt.Name = N'{EscapeSql(targetName)}';");
                sb.AppendLine("END");
                sb.AppendLine();
            }

            foreach (var entity in system.Entities.OrderBy(e => e.SortOrder).ThenBy(e => e.Id))
            {
                var route = $"/s/{system.Slug}/{ToKebab(entity.Name)}";
                sb.AppendLine("IF NOT EXISTS (");
                sb.AppendLine("    SELECT 1 FROM sb.SystemMenus sm");
                sb.AppendLine("    JOIN sb.Systems s ON s.Id = sm.SystemId");
                sb.AppendLine($"    WHERE s.Slug = N'{slug}' AND sm.Route = N'{EscapeSql(route)}'");
                sb.AppendLine(")");
                sb.AppendLine("BEGIN");
                sb.AppendLine("    INSERT INTO sb.SystemMenus (SystemId, Title, Icon, Route, ParentId, SortOrder, IsActive)");
                sb.AppendLine("    SELECT s.Id,");
                sb.AppendLine($"        N'{EscapeSql(entity.DisplayName ?? entity.Name)}',");
                sb.AppendLine("        NULL,");
                sb.AppendLine($"        N'{EscapeSql(route)}',");
                sb.AppendLine("        NULL,");
                sb.AppendLine($"        {entity.SortOrder},");
                sb.AppendLine("        1");
                sb.AppendLine("    FROM sb.Systems s WHERE s.Slug = N'" + slug + "';");
                sb.AppendLine("END");
                sb.AppendLine();
            }

            foreach (var entity in system.Entities.OrderBy(e => e.SortOrder).ThenBy(e => e.Id))
            {
                var entityName = entity.DisplayName ?? entity.Name;
                foreach (var action in Actions)
                {
                    sb.AppendLine("IF NOT EXISTS (");
                    sb.AppendLine("    SELECT 1 FROM sb.Permissions p");
                    sb.AppendLine("    JOIN sb.Systems s ON s.Id = p.SystemId");
                    sb.AppendLine("    JOIN sb.Entities e ON e.SystemId = s.Id");
                    sb.AppendLine($"    WHERE s.Slug = N'{slug}'");
                    sb.AppendLine($"      AND e.Name = N'{EscapeSql(entity.Name)}'");
                    sb.AppendLine($"      AND p.[Key] = CONCAT(e.Id, ':', '{action}')");
                    sb.AppendLine(")");
                    sb.AppendLine("BEGIN");
                    sb.AppendLine("    INSERT INTO sb.Permissions (SystemId, [Key], Description)");
                    sb.AppendLine("    SELECT s.Id,");
                    sb.AppendLine($"        CONCAT(e.Id, ':', '{action}'),");
                    sb.AppendLine($"        N'{EscapeSql($"{entityName} - {ActionLabel(action)}")}'");
                    sb.AppendLine("    FROM sb.Systems s");
                    sb.AppendLine("    JOIN sb.Entities e ON e.SystemId = s.Id");
                    sb.AppendLine($"    WHERE s.Slug = N'{slug}' AND e.Name = N'{EscapeSql(entity.Name)}';");
                    sb.AppendLine("END");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("-- Asignar permisos al rol Admin");
            sb.AppendLine("INSERT INTO sb.RolePermissions (RoleId, PermissionId)");
            sb.AppendLine("SELECT r.Id, p.Id");
            sb.AppendLine("FROM dbo.Roles r");
            sb.AppendLine("JOIN sb.Permissions p ON p.SystemId = (SELECT Id FROM sb.Systems WHERE Slug = N'" + slug + "')");
            sb.AppendLine("WHERE r.Nombre = 'Admin'");
            sb.AppendLine("AND NOT EXISTS (");
            sb.AppendLine("    SELECT 1 FROM sb.RolePermissions rp");
            sb.AppendLine("    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id");
            sb.AppendLine(");");
            sb.AppendLine();

            sb.AppendLine("-- Asignar menus del sistema al rol Admin");
            sb.AppendLine("INSERT INTO sb.SystemMenuRoles (SystemMenuId, RoleId)");
            sb.AppendLine("SELECT sm.Id, r.Id");
            sb.AppendLine("FROM dbo.Roles r");
            sb.AppendLine("JOIN sb.SystemMenus sm ON sm.SystemId = (SELECT Id FROM sb.Systems WHERE Slug = N'" + slug + "')");
            sb.AppendLine("WHERE r.Nombre = 'Admin'");
            sb.AppendLine("AND NOT EXISTS (");
            sb.AppendLine("    SELECT 1 FROM sb.SystemMenuRoles smr");
            sb.AppendLine("    WHERE smr.SystemMenuId = sm.Id AND smr.RoleId = r.Id");
            sb.AppendLine(");");

            return sb.ToString();
        }

        private static string BuildRuntimeSchemaScript(string schemaName, IEnumerable<Entities> entities, IEnumerable<Relations> relations)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{schemaName}')");
            sb.AppendLine($"EXEC('CREATE SCHEMA [{schemaName}]');");

            foreach (var entity in entities.OrderBy(e => e.SortOrder).ThenBy(e => e.Id))
            {
                var tableName = entity.TableName;
                var qualified = $"[{schemaName}].[{tableName}]";

                sb.AppendLine($"IF OBJECT_ID('{qualified}', 'U') IS NULL");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    CREATE TABLE {qualified} (");

                var columnLines = new List<string>();
                var pkColumns = new List<string>();

                foreach (var field in entity.Fields.OrderBy(f => f.SortOrder).ThenBy(f => f.Id))
                {
                    var column = $"        [{field.ColumnName}] {MapSqlType(field)}";
                    if (field.IsIdentity && field.DataType.Equals("int", StringComparison.OrdinalIgnoreCase))
                        column += " IDENTITY(1,1)";

                    var notNull = field.Required || field.IsPrimaryKey || field.IsIdentity;
                    column += notNull ? " NOT NULL" : " NULL";

                    columnLines.Add(column);

                    if (field.IsPrimaryKey)
                        pkColumns.Add($"[{field.ColumnName}]");
                }

                if (pkColumns.Count > 0)
                {
                    var pkName = $"PK_{schemaName}_{tableName}";
                    columnLines.Add($"        CONSTRAINT [{pkName}] PRIMARY KEY ({string.Join(", ", pkColumns)})");
                }

                sb.AppendLine(string.Join(",\n", columnLines));
                sb.AppendLine("    );");
                sb.AppendLine("END");
                sb.AppendLine("ELSE");
                sb.AppendLine("BEGIN");

                foreach (var field in entity.Fields.OrderBy(f => f.SortOrder).ThenBy(f => f.Id))
                {
                    var colName = field.ColumnName;
                    var colType = MapSqlType(field);
                    var nullable = (field.IsIdentity || field.IsPrimaryKey) ? "NOT NULL" : "NULL";
                    var identity = field.IsIdentity && field.DataType.Equals("int", StringComparison.OrdinalIgnoreCase)
                        ? " IDENTITY(1,1)"
                        : "";

                    sb.AppendLine($"    IF COL_LENGTH('{qualified}', '{colName}') IS NULL");
                    sb.AppendLine($"        ALTER TABLE {qualified} ADD [{colName}] {colType}{identity} {nullable};");
                }

                sb.AppendLine("END");

                foreach (var field in entity.Fields.Where(f => f.IsUnique))
                {
                    var uxName = $"UX_{schemaName}_{tableName}_{field.ColumnName}";
                    sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{uxName}' AND object_id = OBJECT_ID('{qualified}'))");
                    sb.AppendLine($"    CREATE UNIQUE INDEX [{uxName}] ON {qualified} ([{field.ColumnName}]);");
                }
            }

            sb.AppendLine(BuildRelationsScript(schemaName, entities, relations));

            return sb.ToString();
        }

        private static string BuildRelationsScript(string schemaName, IEnumerable<Entities> entities, IEnumerable<Relations> relations)
        {
            if (relations == null)
                return string.Empty;

            var entityMap = entities.ToDictionary(e => e.Id, e => e);
            var sb = new StringBuilder();

            foreach (var relation in relations.OrderBy(r => r.Id))
            {
                if (!entityMap.TryGetValue(relation.SourceEntityId, out var source))
                    continue;
                if (!entityMap.TryGetValue(relation.TargetEntityId, out var target))
                    continue;
                if (string.IsNullOrWhiteSpace(relation.ForeignKey))
                    continue;

                var fkColumn = relation.ForeignKey.Trim();
                if (ToSafeSqlName(fkColumn) == null)
                    continue;

                var sourceTable = source.TableName;
                var targetTable = target.TableName;

                var targetPk = target.Fields.FirstOrDefault(f => f.IsPrimaryKey);
                if (targetPk == null)
                    continue;

                var constraintName = $"FK_{schemaName}_{sourceTable}_{targetTable}_{fkColumn}";
                if (constraintName.Length > 120)
                    constraintName = constraintName.Substring(0, 120);

                sb.AppendLine($@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{constraintName}' AND parent_object_id = OBJECT_ID('[{schemaName}].[{sourceTable}]'))
BEGIN
    ALTER TABLE [{schemaName}].[{sourceTable}]
    ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{fkColumn}])
    REFERENCES [{schemaName}].[{targetTable}] ([{targetPk.ColumnName}])
    {(relation.CascadeDelete ? "ON DELETE CASCADE" : "")};
END");
            }

            return sb.ToString();
        }

        private static ExportManifest BuildManifest(Systems system, string schemaName)
        {
            var entities = system.Entities
                .OrderBy(e => e.SortOrder)
                .ThenBy(e => e.Id)
                .ToList();

            var entityMap = entities.ToDictionary(e => e.Id, e => e.Name);

            return new ExportManifest
            {
                System = new ExportSystem
                {
                    Slug = system.Slug,
                    Name = system.Name,
                    Namespace = system.Namespace,
                    Version = system.Version,
                    Description = system.Description,
                    Status = system.Status,
                    IsActive = system.IsActive,
                    Schema = schemaName
                },
                Entities = entities.Select(entity => new ExportEntity
                {
                    Name = entity.Name,
                    DisplayName = entity.DisplayName,
                    TableName = entity.TableName,
                    Description = entity.Description,
                    IsActive = entity.IsActive,
                    SortOrder = entity.SortOrder,
                    Fields = entity.Fields
                        .OrderBy(f => f.SortOrder)
                        .ThenBy(f => f.Id)
                        .Select(field => new ExportField
                        {
                            Name = field.Name,
                            ColumnName = field.ColumnName,
                            DataType = field.DataType,
                            Required = field.Required,
                            MaxLength = field.MaxLength,
                            Precision = field.Precision,
                            Scale = field.Scale,
                            DefaultValue = field.DefaultValue,
                            IsPrimaryKey = field.IsPrimaryKey,
                            IsIdentity = field.IsIdentity,
                            IsUnique = field.IsUnique,
                            UiConfigJson = field.UiConfigJson,
                            SortOrder = field.SortOrder
                        })
                        .ToList()
                }).ToList(),
                Relations = system.Relations
                    .OrderBy(r => r.Id)
                    .Select(relation => new ExportRelation
                    {
                        SourceEntity = entityMap.TryGetValue(relation.SourceEntityId, out var sourceName) ? sourceName : null,
                        TargetEntity = entityMap.TryGetValue(relation.TargetEntityId, out var targetName) ? targetName : null,
                        RelationType = relation.RelationType,
                        ForeignKey = relation.ForeignKey,
                        InverseProperty = relation.InverseProperty,
                        CascadeDelete = relation.CascadeDelete
                    })
                    .ToList()
            };
        }

        private static string BuildReadme(Systems system, string schemaName, bool includeAdminMenus, string adminUser, string adminPassword, string databaseName)
        {
            var version = string.IsNullOrWhiteSpace(system.Version) ? "-" : system.Version.Trim();
            var modo = includeAdminMenus ? "FULL (incluye administracion)" : "RUNTIME-ONLY (solo modulo)";
            return $@"# Export de sistema: {system.Name}

Slug: {system.Slug}
Namespace: {system.Namespace}
Version: {version}
Schema SQL: {schemaName}
Modo: {modo}

## Incluye
- `database.sql`: schema + metadata + runtime.
- `backend/`: API del sistema exportado.
- `frontend/`: UI runtime del sistema exportado.
- `manifest.json`: definicion del sistema.

## Credenciales de prueba
- Usuario: `{adminUser}`
- Password: `{adminPassword}`

## Uso rapido
1. Ejecutar `database.sql` (crea la base `{databaseName}` si no existe).
3. Copiar `backend/.env.example` a `backend/.env` y configurar DB/JWT.
   - Recomendado usar una base vacia para evitar duplicados.
4. Backend:
   - `dotnet restore`
   - `dotnet run` (en carpeta `backend/`)
5. Frontend:
   - `npm install`
   - `npm run dev` (en carpeta `frontend/`)
";
        }

        private static string BuildExportFolderName(Systems system)
        {
            var slug = ToSafeFolderSegment(system.Slug) ?? "system";
            var version = ToSafeFolderSegment(system.Version ?? "0.0") ?? "0.0";
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return $"{slug}_v{version}_{stamp}";
        }

        private static void CopyDirectory(
            string sourceDir,
            string targetDir,
            HashSet<string> excludedDirectories,
            HashSet<string> excludedFiles)
        {
            if (!Directory.Exists(sourceDir))
                return;

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

        private static void ApplyRuntimeOnlyOverlay(string backendPath, string frontendPath)
        {
            // Backend: remove admin controllers and replace with runtime-only versions
            DeleteIfExists(Path.Combine(backendPath, "Controllers", "MenuAdminController.cs"));
            DeleteIfExists(Path.Combine(backendPath, "Controllers", "RolesController.cs"));
            DeleteIfExists(Path.Combine(backendPath, "Controllers", "UsuariosController.cs"));
            DeleteIfExists(Path.Combine(backendPath, "Controllers", "SistemasController.cs"));
            DeleteIfExists(Path.Combine(backendPath, "Controllers", "EntidadesController.cs"));

            var runtimeControllersDir = Path.Combine(backendPath, "Controllers");
            Directory.CreateDirectory(runtimeControllersDir);

            var sistemasRuntimeController = $@"using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{{
    [ApiController]
    [Authorize]
    public class SistemasRuntimeController : AppController
    {{
        [HttpGet(Routes.v1.Sistemas.ObtenerPorSlug)]
        public IActionResult ObtenerPorSlug(string slug)
        {{
            var sistema = SistemasGestor.ObtenerPorSlug(slug);
            return sistema == null ? NotFound() : Ok(sistema);
        }}
    }}
}}
";
            File.WriteAllText(Path.Combine(runtimeControllersDir, "SistemasRuntimeController.cs"), sistemasRuntimeController, new UTF8Encoding(false));

            var entidadesRuntimeController = @"using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class EntidadesRuntimeController : AppController
    {
        [HttpGet(Routes.v1.Entidades.ObtenerRuntime)]
        public IActionResult ObtenerRuntime(int systemId)
        {
            var usuario = UsuarioToken();
            if (usuario.UsuarioId == 0)
                return Unauthorized();

            var entidades = EntidadesGestor.ObtenerParaRuntime(systemId, usuario.UsuarioId);
            return Ok(entidades);
        }
    }
}
";
            File.WriteAllText(Path.Combine(runtimeControllersDir, "EntidadesRuntimeController.cs"), entidadesRuntimeController, new UTF8Encoding(false));

            // runtime template already has runtime-only routing
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static string? ReadMetadataSql(string contentRootPath)
        {
            try
            {
                var path = Path.Combine(contentRootPath, "db", "systembase_metadata.sql");
                return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
            }
            catch
            {
                return null;
            }
        }

        private static string ActionLabel(string action)
        {
            return action switch
            {
                "view" => "Ver",
                "create" => "Crear",
                "edit" => "Editar",
                "delete" => "Eliminar",
                _ => action
            };
        }

        private static string? ToSafeFolderSegment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            var sb = new StringBuilder();

            foreach (var ch in trimmed)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                    sb.Append(ch);
                else if (char.IsWhiteSpace(ch) && sb.Length > 0)
                    sb.Append('-');
            }

            var result = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        private static string MapSqlType(Fields field)
        {
            var type = field.DataType?.ToLowerInvariant();
            return type switch
            {
                "string" => $"NVARCHAR({(field.MaxLength.HasValue && field.MaxLength > 0 ? field.MaxLength.Value.ToString() : "255")})",
                "int" => "INT",
                "decimal" => $"DECIMAL({field.Precision ?? 18},{field.Scale ?? 2})",
                "bool" => "BIT",
                "datetime" => "DATETIME2",
                "guid" => "UNIQUEIDENTIFIER",
                _ => "NVARCHAR(255)"
            };
        }

        private static string? ToSafeSchemaName(string slug)
        {
            var safe = ToSafeSqlName($"sys_{slug}");
            return safe;
        }

        private static string? ToSafeSqlName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var trimmed = input.Trim();
            var sb = new StringBuilder();

            foreach (var ch in trimmed)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    sb.Append(ch);
                }
                else
                {
                    return null;
                }
            }

            if (sb.Length == 0)
                return null;

            if (char.IsDigit(sb[0]))
                return null;

            return sb.ToString();
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

        private static string SqlValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "NULL" : $"N'{EscapeSql(value)}'";
        }

        private static string SqlNumber(int? value)
        {
            return value.HasValue ? value.Value.ToString() : "NULL";
        }

        private static string SqlDate(DateTime? value)
        {
            return value.HasValue ? $"'{value.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL";
        }

        private static string EscapeSql(string? value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        private sealed class ExportManifest
        {
            public ExportSystem System { get; set; } = new();
            public List<ExportEntity> Entities { get; set; } = new();
            public List<ExportRelation> Relations { get; set; } = new();
        }

        private sealed class ExportSystem
        {
            public string Slug { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Namespace { get; set; } = string.Empty;
            public string? Version { get; set; }
            public string? Description { get; set; }
            public string? Status { get; set; }
            public bool IsActive { get; set; }
            public string Schema { get; set; } = string.Empty;
        }

        private sealed class ExportEntity
        {
            public string Name { get; set; } = string.Empty;
            public string? DisplayName { get; set; }
            public string TableName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            public int SortOrder { get; set; }
            public List<ExportField> Fields { get; set; } = new();
        }

        private sealed class ExportField
        {
            public string Name { get; set; } = string.Empty;
            public string ColumnName { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
            public bool Required { get; set; }
            public int? MaxLength { get; set; }
            public int? Precision { get; set; }
            public int? Scale { get; set; }
            public string? DefaultValue { get; set; }
            public bool IsPrimaryKey { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsUnique { get; set; }
            public string? UiConfigJson { get; set; }
            public int SortOrder { get; set; }
        }

        private sealed class ExportRelation
        {
            public string? SourceEntity { get; set; }
            public string? TargetEntity { get; set; }
            public string RelationType { get; set; } = string.Empty;
            public string? ForeignKey { get; set; }
            public string? InverseProperty { get; set; }
            public bool CascadeDelete { get; set; }
        }
    }
}
