namespace Backend.Models.Usuarios
{
    public class UsuarioDetalleResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public int? RolId { get; set; }
        public string Rol { get; set; } = "";

        public bool Activo { get; set; }
    }
}
