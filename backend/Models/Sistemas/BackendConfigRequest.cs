namespace Backend.Models.Sistemas
{
    public class BackendConfigRequest
    {
        public BackendSystemConfig System { get; set; } = new();
        public List<BackendEntityConfig> Entities { get; set; } = new();
    }
}
