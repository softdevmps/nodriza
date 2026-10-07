namespace Backend.Models.Sistemas
{
    public class BackendFieldConfig
    {
        public int FieldId { get; set; }
        public string Name { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public string DataType { get; set; } = null!;
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }

        public bool Expose { get; set; } = true;
        public bool ReadOnly { get; set; }
        public bool? Required { get; set; }
        public int? MaxLength { get; set; }
        public bool? Unique { get; set; }
        public string? DefaultValue { get; set; }
        public string? DisplayAs { get; set; }
    }
}
