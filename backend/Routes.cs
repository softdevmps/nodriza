namespace Backend
{
    public static class Routes
    {
        public static class v1
        {
            public static class Auth
            {
                public const string Login = "api/v1/auth/login";
                public const string Registrar = "api/v1/auth/registrar";
            }

            public static class Menu
            {
                public const string Obtener = "api/v1/menu";
                public const string Tree = "api/v1/menu/tree";
                public const string SidebarTree = "api/v1/menu/sidebar";
                public const string Crear = "api/v1/menu";
                public const string Editar = "api/v1/menu/{id}";
                public const string Desactivar = "api/v1/menu/desactivar/{id}";
            }

            public static class Roles
            {
                public const string Obtener = "api/v1/roles";
                public const string ObtenerPorId = "api/v1/roles/{id}";
                public const string Crear = "api/v1/roles";
                public const string Editar = "api/v1/roles/{id}";
                public const string Estado = "api/v1/roles/{id}/estado";
                public const string AsignarMenus = "api/v1/roles/{id}/menus";
                public const string ObtenerSystemMenus = "api/v1/roles/{id}/system-menus";
                public const string AsignarSystemMenus = "api/v1/roles/{id}/system-menus";
                public const string ObtenerPermisos = "api/v1/roles/{id}/permissions/{systemId}";
                public const string AsignarPermisos = "api/v1/roles/{id}/permissions/{systemId}";
            }

            public static class Usuarios
            {
                public const string Obtener = "api/v1/usuarios";
                public const string ObtenerPorId = "api/v1/usuarios/{id}";
                public const string Crear = "api/v1/usuarios";
                public const string Editar = "api/v1/usuarios/{id}";
                public const string Estado = "api/v1/usuarios/{id}/estado";
            }

            public static class Sistemas
            {
                public const string Obtener = "api/v1/sistemas";
                public const string ObtenerPorId = "api/v1/sistemas/{id}";
                public const string ObtenerPorSlug = "api/v1/sistemas/slug/{slug}";
                public const string Crear = "api/v1/sistemas";
                public const string Editar = "api/v1/sistemas/{id}";
                public const string Eliminar = "api/v1/sistemas/{id}";
                public const string Publicar = "api/v1/sistemas/{id}/publicar";
                public const string Exportar = "api/v1/sistemas/{id}/export";
                public const string EjecutarSql = "api/v1/sistemas/{id}/sql/execute";
                public const string GenerarBackend = "api/v1/sistemas/{id}/generar-backend";
                public const string GenerarFrontend = "api/v1/sistemas/{id}/generar-frontend";
                public const string IniciarBackend = "api/v1/sistemas/{id}/backend/start";
                public const string DetenerBackend = "api/v1/sistemas/{id}/backend/stop";
                public const string PingBackend = "api/v1/sistemas/{id}/backend/ping";
                public const string LogsBackend = "api/v1/sistemas/{id}/backend/logs";
                public const string IniciarFrontend = "api/v1/sistemas/{id}/frontend/start";
                public const string DetenerFrontend = "api/v1/sistemas/{id}/frontend/stop";
                public const string PingFrontend = "api/v1/sistemas/{id}/frontend/ping";
                public const string LogsFrontend = "api/v1/sistemas/{id}/frontend/logs";
            }

            public static class Entidades
            {
                public const string Obtener = "api/v1/sistemas/{systemId}/entidades";
                public const string ObtenerRuntime = "api/v1/sistemas/{systemId}/entidades/runtime";
                public const string ObtenerPorId = "api/v1/sistemas/{systemId}/entidades/{id}";
                public const string ObtenerPorNombre = "api/v1/sistemas/{systemId}/entidades/by-name/{name}";
                public const string Crear = "api/v1/sistemas/{systemId}/entidades";
                public const string Editar = "api/v1/sistemas/{systemId}/entidades/{id}";
                public const string Eliminar = "api/v1/sistemas/{systemId}/entidades/{id}";
            }

            public static class Campos
            {
                public const string Obtener = "api/v1/sistemas/{systemId}/entidades/{entityId}/campos";
                public const string Crear = "api/v1/sistemas/{systemId}/entidades/{entityId}/campos";
                public const string Editar = "api/v1/sistemas/{systemId}/entidades/{entityId}/campos/{id}";
            }

            public static class Datos
            {
                public const string Obtener = "api/v1/sistemas/{systemId}/entidades/{entityId}/datos";
                public const string Crear = "api/v1/sistemas/{systemId}/entidades/{entityId}/datos";
                public const string Editar = "api/v1/sistemas/{systemId}/entidades/{entityId}/datos/{id}";
                public const string Eliminar = "api/v1/sistemas/{systemId}/entidades/{entityId}/datos/{id}";
            }

            public static class Relaciones
            {
                public const string Obtener = "api/v1/sistemas/{systemId}/relaciones";
                public const string Crear = "api/v1/sistemas/{systemId}/relaciones";
                public const string Editar = "api/v1/sistemas/{systemId}/relaciones/{id}";
            }

            public static class Backend
            {
                public const string ObtenerConfig = "api/v1/sistemas/{systemId}/backend-config";
                public const string GuardarConfig = "api/v1/sistemas/{systemId}/backend-config";
            }

            public static class Frontend
            {
                public const string ObtenerConfig = "api/v1/sistemas/{systemId}/frontend-config";
                public const string GuardarConfig = "api/v1/sistemas/{systemId}/frontend-config";
            }

            public static class DevTools
            {
                public const string Restart = "api/v1/dev/restart";
            }

        }
    }
}
