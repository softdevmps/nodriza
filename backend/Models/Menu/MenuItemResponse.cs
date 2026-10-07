namespace Backend.Models.Menu
{
    public class MenuItemResponse
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Icono { get; set; }
        public string Ruta { get; set; }
        public int Orden { get; set; }
        public int? PadreId { get; set; }
    }
}
