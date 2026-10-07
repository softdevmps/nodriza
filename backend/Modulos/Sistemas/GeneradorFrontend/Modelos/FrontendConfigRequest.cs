using Backend.Comun.BaseDeDatos.Tablas;

namespace Backend.Modulos.Sistemas.GeneradorFrontend.Modelos
{
    public class FrontendConfigRequest
    {
        public FrontendSystemConfig System { get; set; } = new();
        public List<FrontendEntityConfig> Entities { get; set; } = new();
    }
}
