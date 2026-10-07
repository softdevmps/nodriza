namespace Backend.Models.Responses.Roles
{
    public class RolSystemMenuResponse
    {
        public int SystemId { get; set; }
        public string SystemName { get; set; } = null!;
        public string SystemSlug { get; set; } = null!;
        public bool Asignado { get; set; }
    }
}
