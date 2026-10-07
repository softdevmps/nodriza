namespace Backend.Models.Sistemas
{
    public class FrontendSystemConfig
    {
        public string AppTitle { get; set; } = "Sistema";
        public string Tagline { get; set; } = "Tu plataforma configurable para gestionar datos en tiempo real.";
        public bool ShowSearch { get; set; } = true;
        public bool ShowFilters { get; set; } = true;
        public int DefaultItemsPerPage { get; set; } = 10;
        public List<int> ItemsPerPageOptions { get; set; } = new() { 10, 20, 50, 100 };
        public string PrimaryColor { get; set; } = "#2563eb";
        public string SecondaryColor { get; set; } = "#0ea5e9";
        public string Density { get; set; } = "comfortable";
        public string FontFamily { get; set; } = "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif";
        public string UiMode { get; set; } = "enterprise";
        public string Locale { get; set; } = "es-AR";
        public string Currency { get; set; } = "ARS";
        public string AuthMode { get; set; } = "local";
        public string AuthBaseUrl { get; set; } = "http://localhost:5032/api/v1";
        public string SeedAdminUser { get; set; } = "admin";
        public string SeedAdminPassword { get; set; } = "admin";
        public string SeedAdminEmail { get; set; } = "admin@local";
    }
}
