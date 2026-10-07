namespace Backend.Models.Responses.Roles
{
    public class RolDetalleResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; }
        public List<RolMenuResponse> Menus { get; set; } = new();
    }

    public class RolMenuResponse
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = null!;
        public string Icono { get; set; } = null!;
        public string? Ruta { get; set; }
        public bool Asignado { get; set; }
    }
}
