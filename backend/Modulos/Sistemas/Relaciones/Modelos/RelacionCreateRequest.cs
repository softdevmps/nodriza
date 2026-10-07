using System.ComponentModel.DataAnnotations;
using Backend.Comun;

namespace Backend.Modulos.Sistemas.Relaciones.Modelos
{
    public class RelacionCreateRequest
    {
        [Required]
        public int SourceEntityId { get; set; }

        [Required]
        public int TargetEntityId { get; set; }

        [Required]
        public string RelationType { get; set; }

        [Required]
        [IdentificadorSql]
        public string ForeignKey { get; set; } = string.Empty;

        [IdentificadorSql(Opcional = true)]
        public string? InverseProperty { get; set; }

        public bool CascadeDelete { get; set; }
    }
}
