namespace Backend.Models.Sistemas
{
    public class BackendEntityConfig
    {
        public int EntityId { get; set; }
        public string Name { get; set; } = null!;
        public string? DisplayName { get; set; }

        public bool IsEnabled { get; set; } = true;
        public string Route { get; set; } = string.Empty;
        public bool? RequireAuth { get; set; }

        public bool SoftDelete { get; set; }
        public int? SoftDeleteFieldId { get; set; }

        public bool Pagination { get; set; }
        public int? DefaultPageSize { get; set; }
        public int? MaxPageSize { get; set; }

        public BackendEndpointsConfig Endpoints { get; set; } = new();

        public int? DefaultSortFieldId { get; set; }
        public string? DefaultSortDirection { get; set; }

        public List<int> FilterFieldIds { get; set; } = new();
        public List<BackendFieldConfig> Fields { get; set; } = new();
    }
}
