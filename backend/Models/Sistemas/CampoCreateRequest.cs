using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Sistemas
{
    public class CampoCreateRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string ColumnName { get; set; }

        [Required]
        public string DataType { get; set; }

        public bool Required { get; set; }

        public int? MaxLength { get; set; }

        public int? Precision { get; set; }

        public int? Scale { get; set; }

        public string? DefaultValue { get; set; }

        public bool IsPrimaryKey { get; set; }

        public bool IsIdentity { get; set; }

        public bool IsUnique { get; set; }

        public string? UiConfigJson { get; set; }

        public int SortOrder { get; set; } = 1;
    }
}
