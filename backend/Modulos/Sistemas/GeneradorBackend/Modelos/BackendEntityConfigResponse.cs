namespace Backend.Modulos.Sistemas.GeneradorBackend.Modelos
{
    public class BackendEntityConfigResponse
    {
        public int EntityId { get; set; }
        public string Name { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsEnabled { get; set; }
    }
}
