namespace Backend.Models.Sistemas
{
    public class FrontendFieldConfig
    {
        public int FieldId { get; set; }
        public string Name { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public string? DataType { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public bool Required { get; set; }
        public int? MaxLength { get; set; }
        public string? Label { get; set; }
        public bool ShowInList { get; set; } = true;
        public bool ShowInForm { get; set; } = true;
        public bool ShowInFilter { get; set; } = true;
        public string? Placeholder { get; set; }
        public string? HelpText { get; set; }
        public string? InputType { get; set; }
        public string? Section { get; set; }
        public string? Format { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public string? Pattern { get; set; }
        public bool QuickToggle { get; set; }
    }
}
