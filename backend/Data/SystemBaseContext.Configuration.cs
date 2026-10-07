using DotNetEnv;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

// Separado del archivo scaffoldeado (scaffold-all.sh usa --no-onconfiguring)
// para que `new SystemBaseContext()` use la misma conexion que el .env.
public partial class SystemBaseContext
{
    static SystemBaseContext()
    {
        Env.Load();
    }

    public static string BuildConnectionString()
    {
        var server = Environment.GetEnvironmentVariable("DB_SERVER");
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var trustCert = Environment.GetEnvironmentVariable("DB_TRUST_CERT") ?? "True";

        return $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate={trustCert};";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(BuildConnectionString());
    }
}
