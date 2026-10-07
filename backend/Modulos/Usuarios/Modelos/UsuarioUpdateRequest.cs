using System.ComponentModel.DataAnnotations;

namespace Backend.Modulos.Usuarios.Modelos
{
    public class UsuarioUpdateRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        public string? Password { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Apellido { get; set; } = null!;

        [Range(1, int.MaxValue, ErrorMessage = "Elegí un rol.")]
        public int RolId { get; set; }
    }
}
