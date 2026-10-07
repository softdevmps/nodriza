using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;

namespace Backend.Modulos.Sistemas.GeneradorBackend
{
    /// <summary>
    /// Credenciales SQL de mínimo privilegio por sistema:
    /// - nodriza_&lt;slug&gt;: login del backend generado. CRUD solo en sys_&lt;slug&gt; y, de dbo.Usuarios,
    ///   lo justo para el login (lectura de 5 columnas) y el registro (alta).
    /// - nodriza_consola_&lt;slug&gt;: usuario SIN login con el que corre la consola SQL (EXECUTE AS).
    ///   Puede crear y modificar objetos solo dentro de sys_&lt;slug&gt;.
    /// Así un sistema generado (o un script de consola) no puede tocar la fábrica ni otros sistemas.
    /// </summary>
    public static class CredencialesSistema
    {
        public static string NombreLogin(string slug) => $"nodriza_{slug}";

        public static string NombreUsuarioConsola(string slug) => $"nodriza_consola_{slug}";

        /// <summary>Contraseña aleatoria que cumple la política de SQL Server (mayúsculas, minúsculas, dígitos y símbolos).</summary>
        public static string GenerarPassword()
        {
            const string grupos = "ABCDEFGHJKLMNPQRSTUVWXYZ|abcdefghijkmnpqrstuvwxyz|23456789|-_.";
            var partes = grupos.Split('|');
            var todos = string.Concat(partes);
            var chars = new char[32];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = todos[RandomNumberGenerator.GetInt32(todos.Length)];
            for (var g = 0; g < partes.Length; g++)
                chars[g] = partes[g][RandomNumberGenerator.GetInt32(partes[g].Length)];
            return new string(chars);
        }

        /// <summary>Secreto JWT propio del sistema (512 bits).</summary>
        public static string GenerarSecretoJwt() => Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        /// <summary>Crea (o actualiza la contraseña de) el login del sistema y le da permisos mínimos.</summary>
        public static void AsegurarLogin(SystemBaseContext context, string slug, string password)
        {
            const string sql = @"
DECLARE @login SYSNAME = @p0, @pass NVARCHAR(128) = @p1, @schema SYSNAME = @p2, @s NVARCHAR(MAX);
IF SCHEMA_ID(@schema) IS NULL
BEGIN
    SET @s = N'CREATE SCHEMA ' + QUOTENAME(@schema);
    EXEC sp_executesql @s;
END
IF SUSER_ID(@login) IS NULL
    SET @s = N'CREATE LOGIN ' + QUOTENAME(@login) + N' WITH PASSWORD = ' + QUOTENAME(@pass, '''') +
             N', CHECK_POLICY = ON, DEFAULT_DATABASE = ' + QUOTENAME(DB_NAME()) + N';';
ELSE
    SET @s = N'ALTER LOGIN ' + QUOTENAME(@login) + N' WITH PASSWORD = ' + QUOTENAME(@pass, '''') + N'; ' +
             N'ALTER LOGIN ' + QUOTENAME(@login) + N' ENABLE;';
EXEC sp_executesql @s;
IF USER_ID(@login) IS NULL
BEGIN
    SET @s = N'CREATE USER ' + QUOTENAME(@login) + N' FOR LOGIN ' + QUOTENAME(@login) + N' WITH DEFAULT_SCHEMA = ' + QUOTENAME(@schema) + N';';
    EXEC sp_executesql @s;
END
SET @s = N'GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::' + QUOTENAME(@schema) + N' TO ' + QUOTENAME(@login) + N'; ' +
         N'GRANT SELECT (Id, Username, Email, PasswordHash, Activo) ON dbo.Usuarios TO ' + QUOTENAME(@login) + N'; ' +
         N'GRANT INSERT ON dbo.Usuarios TO ' + QUOTENAME(@login) + N';';
EXEC sp_executesql @s;";

            context.Database.ExecuteSqlRaw(sql,
                new SqlParameter("@p0", NombreLogin(slug)),
                new SqlParameter("@p1", password),
                new SqlParameter("@p2", $"sys_{slug}"));
        }

        /// <summary>Usuario sin login para la consola SQL: solo puede operar dentro de sys_&lt;slug&gt;.</summary>
        public static void AsegurarUsuarioConsola(SystemBaseContext context, string slug)
        {
            const string sql = @"
DECLARE @usuario SYSNAME = @p0, @schema SYSNAME = @p1, @s NVARCHAR(MAX);
IF SCHEMA_ID(@schema) IS NULL
BEGIN
    SET @s = N'CREATE SCHEMA ' + QUOTENAME(@schema);
    EXEC sp_executesql @s;
END
IF USER_ID(@usuario) IS NULL
BEGIN
    SET @s = N'CREATE USER ' + QUOTENAME(@usuario) + N' WITHOUT LOGIN WITH DEFAULT_SCHEMA = ' + QUOTENAME(@schema) + N';';
    EXEC sp_executesql @s;
END
SET @s = N'GRANT CREATE TABLE, CREATE VIEW, CREATE PROCEDURE, CREATE FUNCTION TO ' + QUOTENAME(@usuario) + N'; ' +
         N'GRANT ALTER, SELECT, INSERT, UPDATE, DELETE, REFERENCES, EXECUTE ON SCHEMA::' + QUOTENAME(@schema) + N' TO ' + QUOTENAME(@usuario) + N';';
EXEC sp_executesql @s;";

            context.Database.ExecuteSqlRaw(sql,
                new SqlParameter("@p0", NombreUsuarioConsola(slug)),
                new SqlParameter("@p1", $"sys_{slug}"));
        }

        /// <summary>Al eliminar un sistema: se borran su login y sus usuarios de base (los datos quedan archivados).</summary>
        public static void Eliminar(SystemBaseContext context, string slug)
        {
            const string sql = @"
DECLARE @login SYSNAME = @p0, @consola SYSNAME = @p1, @s NVARCHAR(MAX);
IF USER_ID(@login) IS NOT NULL BEGIN SET @s = N'DROP USER ' + QUOTENAME(@login); EXEC sp_executesql @s; END
IF USER_ID(@consola) IS NOT NULL BEGIN SET @s = N'DROP USER ' + QUOTENAME(@consola); EXEC sp_executesql @s; END
IF SUSER_ID(@login) IS NOT NULL
BEGIN
    -- Cierra sesiones abiertas del login (conexiones del pool de un backend ya detenido).
    DECLARE @kill NVARCHAR(MAX) = N'';
    SELECT @kill = @kill + N'KILL ' + CAST(session_id AS NVARCHAR(10)) + N';'
    FROM sys.dm_exec_sessions WHERE login_name = @login;
    IF LEN(@kill) > 0 EXEC sp_executesql @kill;
    SET @s = N'DROP LOGIN ' + QUOTENAME(@login);
    EXEC sp_executesql @s;
END";

            context.Database.ExecuteSqlRaw(sql,
                new SqlParameter("@p0", NombreLogin(slug)),
                new SqlParameter("@p1", NombreUsuarioConsola(slug)));
        }
    }
}
