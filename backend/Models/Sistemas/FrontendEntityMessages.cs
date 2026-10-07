namespace Backend.Models.Sistemas
{
    public class FrontendEntityMessages
    {
        public string Empty { get; set; } = "No hay registros todavia.";
        public string Error { get; set; } = "Ocurrio un error al procesar la solicitud.";
        public string SuccessCreate { get; set; } = "Registro creado.";
        public string SuccessUpdate { get; set; } = "Registro actualizado.";
        public string SuccessDelete { get; set; } = "Registro eliminado.";
    }
}
