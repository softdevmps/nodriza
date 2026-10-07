using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Roles.Modelos
{
    public class RolResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; }
    }
}
