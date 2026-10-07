using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Sistemas
{
    public class SistemaUpdateRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Namespace { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Version { get; set; }
    }
}
