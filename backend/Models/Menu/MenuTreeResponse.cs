namespace Backend.Models.Menu
{
    public class MenuTreeResponse
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Icono { get; set; }
        public string? Ruta { get; set; }
        public int Orden { get; set; }

        public List<MenuTreeResponse> Children { get; set; } = new();
    }
}
