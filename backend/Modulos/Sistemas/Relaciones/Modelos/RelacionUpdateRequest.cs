using System.ComponentModel.DataAnnotations;
using Backend.Comun;

namespace Backend.Modulos.Sistemas.Relaciones.Modelos
{
    public class RelacionUpdateRequest
    {
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
