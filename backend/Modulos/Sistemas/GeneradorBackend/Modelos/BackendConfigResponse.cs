using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Sistemas.GeneradorBackend.Modelos
{
    public class BackendConfigResponse
    {
        public BackendSystemConfig System { get; set; } = new();
        public List<BackendEntityConfig> Entities { get; set; } = new();
    }
}
