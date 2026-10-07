using System.ComponentModel.DataAnnotations;

namespace Backend.Modulos.Sistemas.Modelos
{
    public class SqlScriptExecuteRequest
    {
        [Required]
        public string Script { get; set; } = string.Empty;

        public bool ImportMetadata { get; set; } = true;
    }
}
