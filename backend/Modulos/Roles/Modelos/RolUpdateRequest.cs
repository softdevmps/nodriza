using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Roles.Modelos
{
    public class RolUpdateRequest
    {
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; }
    }
}
