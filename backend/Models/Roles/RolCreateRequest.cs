namespace Backend.Models.Requests.Roles
{
    public class RolCreateRequest
    {
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; } = true;
    }
}
