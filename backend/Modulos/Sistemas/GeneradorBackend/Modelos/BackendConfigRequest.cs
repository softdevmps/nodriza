using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Sistemas.GeneradorBackend.Modelos
{
    public class BackendConfigRequest
    {
        public BackendSystemConfig System { get; set; } = new();
        public List<BackendEntityConfig> Entities { get; set; } = new();
    }
}
