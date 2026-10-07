using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Usuarios.Modelos
{
    public class UsuarioCreateRequest
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public int RolId { get; set; }
    }
}
