namespace Backend.Models.Sistemas
{
    public class BackendEndpointConfig
    {
        public bool? RequireAuth { get; set; }
        public bool? UseSoftDelete { get; set; }
    }

    public class BackendEndpointsConfig
    {
        public bool List { get; set; } = true;
        public bool Get { get; set; } = true;
        public bool Create { get; set; } = true;
        public bool Update { get; set; } = true;
        public bool Delete { get; set; } = true;

        public BackendEndpointConfig? ListConfig { get; set; }
        public BackendEndpointConfig? GetConfig { get; set; }
        public BackendEndpointConfig? CreateConfig { get; set; }
        public BackendEndpointConfig? UpdateConfig { get; set; }
        public BackendEndpointConfig? DeleteConfig { get; set; }
    }
}
