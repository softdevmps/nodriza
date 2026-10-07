using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Menu
{
    public class MenuRequest
    {
        [Required]
        public string Titulo { get; set; }

        [Required]
        public string Icono { get; set; }

        public string? Ruta { get; set; }

        public int Orden { get; set; }

        public int? PadreId { get; set; }

        public List<int> RolesIds { get; set; } = new();
    }
}
