using Backend.Utils;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Models.Jwt
{
    public static class JwtService
    {
        public static (string token, DateTime expiracion) GenerarToken(int usuarioId, string usuario)
        {
            var claims = new[]
            {
                new Claim("usuarioId", usuarioId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(AppConfig.JWT_SECRET)
            );

            var token = new JwtSecurityToken(
                issuer: AppConfig.JWT_ISSUER,
                audience: AppConfig.JWT_AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(AppConfig.JWT_EXPIRE_MINUTES),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return (
                new JwtSecurityTokenHandler().WriteToken(token),
                DateTime.UtcNow.AddMinutes(AppConfig.JWT_EXPIRE_MINUTES)
            );
        }
    }
}
