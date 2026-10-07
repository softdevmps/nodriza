using System.ComponentModel.DataAnnotations;

namespace Backend.Modulos.Auth.Modelos
{
    public class RegistrarRequest
    {
        [Required, StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required, StringLength(100)]
        public string Nombre { get; set; }

        [Required, StringLength(100)]
        public string Apellido { get; set; }
    }
}
