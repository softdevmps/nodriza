namespace Backend.Models.Responses.Roles
{
    public class RolResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; }
    }
}
