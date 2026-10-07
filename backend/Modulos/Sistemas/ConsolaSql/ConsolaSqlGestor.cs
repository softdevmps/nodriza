using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Backend.Comun.BaseDeDatos;
using Backend.Modulos.Sistemas.GeneradorBackend;

namespace Backend.Modulos.Sistemas.ConsolaSql
{
    /// <summary>
    /// Consola SQL de bootstrap (solo DEV y admin): valida el script, lo corre aislado en sys_&lt;slug&gt;
    /// (EXECUTE AS un usuario sin login, ver CredencialesSistema) y opcionalmente sincroniza la metadata.
    /// </summary>
    public static class ConsolaSqlGestor
    {
        /// <summary>Devuelve (Ok, Error, Resultado). El error de SQL va tal cual: es el script del propio admin.</summary>
        public static (bool Ok, string? Error, object? Resultado) Ejecutar(int systemId, string slug, string? scriptOriginal, bool importarMetadata)
        {
            var script = NormalizeSqlScript(scriptOriginal);
            if (string.IsNullOrWhiteSpace(script))
                return (false, "El script SQL esta vacio.", null);

            var schema = $"sys_{slug}".ToLowerInvariant();
            var validation = ValidateSqlScript(script, schema);
            if (!validation.Ok)
                return (false, validation.Error, null);

            var batches = SplitSqlBatches(script);
            if (batches.Count == 0)
                return (false, "No se detectaron sentencias SQL ejecutables.", null);

            using var context = new SystemBaseContext();

            // El script NO corre como la cuenta de la fábrica: corre como un usuario de base sin login
            // que solo tiene permisos en sys_<slug>. Aunque se evada el filtro (p. ej. con SQL dinámico),
            // la base le niega todo lo que esté fuera del schema del sistema.
            CredencialesSistema.AsegurarUsuarioConsola(context, slug);

            using var trx = context.Database.BeginTransaction();
            byte[]? cookie = null;
            try
            {
                cookie = (byte[])EjecutarAdHoc(context,
                    $"DECLARE @c VARBINARY(8000); EXECUTE AS USER = N'{CredencialesSistema.NombreUsuarioConsola(slug)}' WITH COOKIE INTO @c; SELECT @c;")!;

                var executed = 0;
                try
                {
                    foreach (var batch in batches)
                    {
                        context.Database.ExecuteSqlRaw(batch);
                        executed++;
                    }
                }
                finally
                {
                    // Volver a la cuenta de la fábrica (solo con la cookie: el script no puede hacer REVERT).
                    RevertirContexto(context, cookie);
                    cookie = null;
                }

                SincronizadorMetadata.MetadataSyncResult? metadata = null;
                if (importarMetadata)
                    metadata = SincronizadorMetadata.Sincronizar(context, systemId, schema);

                var message = $"Script SQL ejecutado. Batches: {executed}.";
                if (metadata != null)
                {
                    message += $" Metadata: entidades +{metadata.EntitiesCreated}/{metadata.EntitiesUpdated}, campos +{metadata.FieldsCreated}/{metadata.FieldsUpdated}, relaciones +{metadata.RelationsCreated}/{metadata.RelationsUpdated}.";
                }

                trx.Commit();
                return (true, null, new
                {
                    message,
                    schema,
                    batches = executed,
                    metadata
                });
            }
            catch (Exception ex)
            {
                if (cookie != null)
                    RevertirContexto(context, cookie);
                trx.Rollback();
                // Es el script del propio admin (consola DEV): el error de SQL le sirve para corregirlo.
                return (false, $"Error ejecutando SQL: {ex.Message}", null);
            }
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

            var forbiddenPattern = @"\b(use|backup|restore|shutdown|reconfigure|sp_configure|xp_cmdshell|exec\s+xp_|create\s+database|drop\s+database|alter\s+database|create\s+login|drop\s+login|alter\s+login|revert|setuser|execute\s+as|exec\s+as)\b";
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

        /// <summary>
        /// EXECUTE AS ... WITH COOKIE y REVERT solo se permiten en un lote ad hoc (sin sp_executesql),
        /// así que van sin parámetros. Los valores interpolados son seguros: un nombre derivado del slug
        /// validado ([a-z0-9_]) y la cookie en hexadecimal.
        /// </summary>
        private static object? EjecutarAdHoc(SystemBaseContext context, string sql)
        {
            var conn = context.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            return cmd.ExecuteScalar();
        }

        private static void RevertirContexto(SystemBaseContext context, byte[] cookie)
        {
            try
            {
                EjecutarAdHoc(context, $"REVERT WITH COOKIE = 0x{Convert.ToHexString(cookie)};");
            }
            catch
            {
                // Si la conexión quedó inutilizable, el pool la resetea (sp_reset_connection) al devolverla.
            }
        }
    }
}
