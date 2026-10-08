using Microsoft.Data.SqlClient;
using Backend.Utils;

namespace Backend.Data
{
    public static class Db
    {
        public static SqlConnection Open()
        {
            var connectionString =
                $"Server={AppConfig.DB_SERVER};Database={AppConfig.DB_NAME};User Id={AppConfig.DB_USER};Password={AppConfig.DB_PASSWORD};TrustServerCertificate=True;";

            var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }
    }
}
