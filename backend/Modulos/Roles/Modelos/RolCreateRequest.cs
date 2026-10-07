using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Roles.Modelos
{
    public class RolCreateRequest
    {
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; } = true;
    }
}
