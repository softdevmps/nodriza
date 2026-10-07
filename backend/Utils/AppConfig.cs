using DotNetEnv;

namespace Backend.Utils
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
    }
}
