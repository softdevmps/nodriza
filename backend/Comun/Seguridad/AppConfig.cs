using DotNetEnv;

namespace Backend.Comun.Seguridad
{
    public static class AppConfig
    {
        static AppConfig()
        {
            Env.Load();
        }

        public static string JWT_SECRET =>
            Environment.GetEnvironmentVariable("JWT_SECRET");

        public static string JWT_ISSUER =>
            Environment.GetEnvironmentVariable("JWT_ISSUER");

        public static string JWT_AUDIENCE =>
            Environment.GetEnvironmentVariable("JWT_AUDIENCE");

        public static int JWT_EXPIRE_MINUTES =>
            int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES"));

        /// <summary>Registro público de usuarios. Cerrado por defecto: el alta la hace un admin.</summary>
        public static bool REGISTRO_PUBLICO =>
            bool.TryParse(Environment.GetEnvironmentVariable("REGISTRO_PUBLICO"), out var habilitado) && habilitado;
    }
}
