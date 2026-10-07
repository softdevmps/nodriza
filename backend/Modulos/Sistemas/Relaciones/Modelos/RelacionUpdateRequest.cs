using System.ComponentModel.DataAnnotations;

namespace Backend.Modulos.Sistemas.Relaciones.Modelos
{
    public class RelacionUpdateRequest
    {
        [Required]
        public string RelationType { get; set; }

        public string? ForeignKey { get; set; }

        public string? InverseProperty { get; set; }

        public bool CascadeDelete { get; set; }
    }
}
