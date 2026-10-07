namespace Backend.Models.Sistemas
{
    public class RelacionResponse
    {
        public int Id { get; set; }
        public int SystemId { get; set; }
        public int SourceEntityId { get; set; }
        public int TargetEntityId { get; set; }
        public string RelationType { get; set; }
        public string? ForeignKey { get; set; }
        public string? InverseProperty { get; set; }
        public bool CascadeDelete { get; set; }
    }
}
