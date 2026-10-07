using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Sistemas
{
    public class RelacionCreateRequest
    {
        [Required]
        public int SourceEntityId { get; set; }

        [Required]
        public int TargetEntityId { get; set; }

        [Required]
        public string RelationType { get; set; }

        public string? ForeignKey { get; set; }

        public string? InverseProperty { get; set; }

        public bool CascadeDelete { get; set; }
    }
}
