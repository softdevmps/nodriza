namespace Backend.Models.Sistemas
{
    public class SistemaResponse
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
    }
}
