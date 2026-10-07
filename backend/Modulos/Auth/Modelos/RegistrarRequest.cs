using System.ComponentModel.DataAnnotations;

namespace Backend.Modulos.Auth.Modelos
{
    public class RegistrarRequest
    {
        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido { get; set; }
    }
}
