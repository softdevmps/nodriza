namespace Backend.Models.Auth
{
    public class LoginResponse
    {
        public int UsuarioId { get; set; }
        public string Usuario { get; set; }
        public string Token { get; set; }
        public DateTime Expiracion { get; set; }
    }
}
