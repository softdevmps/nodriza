namespace Backend.Modulos.Sistemas.Datos
{
    /// <summary>
    /// Parámetros de GET .../datos: ?take=25&amp;skip=50&amp;buscar=ana&amp;filtroCampo=Email&amp;filtroValor=gmail&amp;ordenarPor=Nombre&amp;orden=desc
    /// Todos son opcionales. Sin take se devuelven todos los registros.
    /// </summary>
    public class ConsultaDatos
    {
        public int? Take { get; set; }
        public int? Skip { get; set; }
        public string? Buscar { get; set; }
        public string? FiltroCampo { get; set; }
        public string? FiltroValor { get; set; }
        public string? OrdenarPor { get; set; }
        public string? Orden { get; set; }
    }
}
