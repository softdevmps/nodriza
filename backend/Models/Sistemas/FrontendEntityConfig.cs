namespace Backend.Models.Sistemas
{
    public class FrontendEntityConfig
    {
        public int EntityId { get; set; }
        public string Name { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? MenuLabel { get; set; }
        public bool ShowInMenu { get; set; } = true;
        public string? MenuIcon { get; set; }
        public string? RouteSlug { get; set; }
        public bool ListStickyHeader { get; set; }
        public bool ListShowTotals { get; set; } = true;
        public int? DefaultSortFieldId { get; set; }
        public string DefaultSortDirection { get; set; } = "asc";
        public string FormLayout { get; set; } = "single";
        public bool ConfirmSave { get; set; } = true;
        public bool ConfirmDelete { get; set; } = true;
        public bool EnableDuplicate { get; set; } = true;
        public FrontendEntityMessages Messages { get; set; } = new();
        public List<FrontendFieldConfig> Fields { get; set; } = new();
    }
}
