using Backend.Modulos.Auth;
using Backend.Modulos.Menu;
using Backend.Modulos.Roles;
using Backend.Modulos.Sistemas;
using Backend.Modulos.Sistemas.Campos;
using Backend.Modulos.Sistemas.ConsolaSql;
using Backend.Modulos.Sistemas.Datos;
using Backend.Modulos.Sistemas.Entidades;
using Backend.Modulos.Sistemas.Exportacion;
using Backend.Modulos.Sistemas.GeneradorBackend;
using Backend.Modulos.Sistemas.GeneradorFrontend;
using Backend.Modulos.Sistemas.Herramientas;
using Backend.Modulos.Sistemas.Publicacion;
using Backend.Modulos.Sistemas.Relaciones;
using Backend.Modulos.Usuarios;

namespace Backend.Comun
{
    /// <summary>
    /// Registro de los gestores en el contenedor de dependencias.
    /// Son singletons sin estado: cada operación abre su propio SystemBaseContext con
    /// IDbContextFactory (igual que antes con `new SystemBaseContext()`), así que no comparten
    /// tracking ni transacciones entre requests. Los controllers los reciben por constructor.
    /// Al crear un gestor nuevo, registralo acá.
    /// </summary>
    public static class Inyeccion
    {
        public static IServiceCollection AddGestores(this IServiceCollection services)
        {
            services.AddSingleton<AuthGestor>();
            services.AddSingleton<UsuariosGestor>();
            services.AddSingleton<RolesGestor>();
            services.AddSingleton<MenuGestor>();

            services.AddSingleton<SistemasGestor>();
            services.AddSingleton<EntidadesGestor>();
            services.AddSingleton<CamposGestor>();
            services.AddSingleton<RelacionesGestor>();
            services.AddSingleton<DatosGestor>();
            services.AddSingleton<SistemasPublicador>();
            services.AddSingleton<ConsolaSqlGestor>();

            services.AddSingleton<BackendConfigGestor>();
            services.AddSingleton<FrontendConfigGestor>();
            services.AddSingleton<SistemasBackendGenerator>();
            services.AddSingleton<SistemasFrontendGenerator>();
            services.AddSingleton<SistemasExportador>();
            services.AddSingleton<ProcesosSistemas>();

            return services;
        }
    }
}
