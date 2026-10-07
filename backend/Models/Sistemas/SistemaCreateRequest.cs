using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Sistemas
{
    public class SistemaCreateRequest
    {
        [Required]
        public string Slug { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Namespace { get; set; }

        public string? Description { get; set; }

        public string? Version { get; set; }
    }
}
