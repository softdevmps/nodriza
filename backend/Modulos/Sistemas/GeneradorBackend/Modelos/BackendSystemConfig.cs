namespace Backend.Modulos.Sistemas.GeneradorBackend.Modelos
{
    public class BackendSystemConfig
    {
        public string ApiBase { get; set; } = "api/v1";
        public bool RequireAuth { get; set; } = true;
        public string SchemaPrefix { get; set; } = "sys";
        public string Persistence { get; set; } = "sql";
        public int DefaultPageSize { get; set; } = 50;
        public int MaxPageSize { get; set; } = 200;
    }
}
