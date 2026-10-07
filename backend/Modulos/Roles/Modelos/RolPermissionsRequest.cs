using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Roles.Modelos
{
    public class RolPermissionsRequest
    {
        public List<int> PermissionIds { get; set; } = new();
    }
}
