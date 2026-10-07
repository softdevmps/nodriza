using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Roles.Modelos
{
    public class RolMenusRequest
    {
        public List<int> MenusIds { get; set; } = new();
    }
}
