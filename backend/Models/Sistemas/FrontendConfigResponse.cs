namespace Backend.Models.Sistemas
{
    public class FrontendConfigResponse
    {
        public FrontendSystemConfig System { get; set; } = new();
        public List<FrontendEntityConfig> Entities { get; set; } = new();
    }
}
