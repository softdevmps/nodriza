using System.ComponentModel.DataAnnotations;
using Backend.Comun;

namespace Backend.Modulos.Sistemas.Entidades.Modelos
{
    public class EntidadUpdateRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [IdentificadorSql]
        public string TableName { get; set; }

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public int SortOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}
