namespace Backend.Models.Sistemas
{
    public class EntidadResponse
    {
        public int Id { get; set; }
        public int SystemId { get; set; }
        public string Name { get; set; }
        public string TableName { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }
}
