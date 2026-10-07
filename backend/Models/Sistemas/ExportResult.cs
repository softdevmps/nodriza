namespace Backend.Models.Sistemas
{
    public class ExportResult
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public string? ExportPath { get; set; }
        public string? ZipPath { get; set; }
        public string? ZipFileName { get; set; }
        public List<string> Files { get; set; } = new();
    }
}
