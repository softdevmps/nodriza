namespace Backend.Models.Requests.Roles
{
    public class RolUpdateRequest
    {
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; }
    }
}
