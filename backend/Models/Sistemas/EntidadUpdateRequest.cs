using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Sistemas
{
    public class EntidadUpdateRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string TableName { get; set; }

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public int SortOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}
