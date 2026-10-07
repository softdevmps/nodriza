using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Roles.Modelos
{
    public class RolPermissionResponse
    {
        public int PermissionId { get; set; }
        public int EntityId { get; set; }
        public string EntityName { get; set; } = null!;
        public string Action { get; set; } = null!;
        public bool Asignado { get; set; }
    }
}
