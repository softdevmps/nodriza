namespace Backend.Models.Sistemas
{
    public class BackendConfigResponse
    {
        public BackendSystemConfig System { get; set; } = new();
        public List<BackendEntityConfig> Entities { get; set; } = new();
    }
}
