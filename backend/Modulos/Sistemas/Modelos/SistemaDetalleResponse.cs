using Backend.Comun;
namespace Backend.Modulos.Sistemas.Modelos
{
    public class SistemaDetalleResponse
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        /// <summary>Puertos locales del backend y frontend generados (ver Comun/PuertosSistemas).</summary>
        public int PuertoBackend => PuertosSistemas.Backend(Id);
        public int PuertoFrontend => PuertosSistemas.Frontend(Id);
    }
}
