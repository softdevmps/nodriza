namespace Backend.Models.Sistemas
{
    public class FrontendConfigRequest
    {
        public FrontendSystemConfig System { get; set; } = new();
        public List<FrontendEntityConfig> Entities { get; set; } = new();
    }
}
