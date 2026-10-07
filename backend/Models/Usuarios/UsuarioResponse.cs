namespace Backend.Models.Usuarios
{
    public class UsuarioResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;

        public int? RolId { get; set; }  
        public string Rol { get; set; } = "";

        public bool Activo { get; set; }
    }
}
