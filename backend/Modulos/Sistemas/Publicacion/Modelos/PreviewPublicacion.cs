namespace Backend.Modulos.Sistemas.Publicacion.Modelos
{
    /// <summary>Lo que haría "Publicar", para mostrarlo y pedir confirmación antes.</summary>
    public class PreviewPublicacion
    {
        /// <summary>false si hay errores (Publicar los rechazaría) o si el sistema no se puede publicar (Message).</summary>
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public List<string> Errores { get; set; } = new();
        public List<string> TablasNuevas { get; set; } = new();
        /// <summary>"Tabla Clientes → Personas", "Personas.Nombre → NombreCompleto".</summary>
        public List<string> Renombres { get; set; } = new();
        /// <summary>SQL que se ejecutaría sobre tablas existentes, en orden.</summary>
        public List<string> Cambios { get; set; } = new();
    }
}
