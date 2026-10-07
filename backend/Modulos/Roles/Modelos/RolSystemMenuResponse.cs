using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Roles.Modelos
{
    public class RolSystemMenuResponse
    {
        public int SystemId { get; set; }
        public string SystemName { get; set; } = null!;
        public string SystemSlug { get; set; } = null!;
        public bool Asignado { get; set; }
    }
}
