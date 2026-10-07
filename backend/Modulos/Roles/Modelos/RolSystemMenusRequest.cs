using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Roles.Modelos
{
    public class RolSystemMenusRequest
    {
        public List<int> SystemIds { get; set; } = new();
    }
}
