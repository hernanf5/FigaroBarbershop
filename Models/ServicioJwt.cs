using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FigaroBarbershop.Models
{
    public class ServicioJwt
    {
        private readonly IConfiguration config;

        public ServicioJwt(IConfiguration config) => this.config = config;

        public (string Token, DateTime Expira) Generar(Usuario usuario)
        {
            var expira = DateTime.UtcNow.AddHours(config.GetValue("Jwt:HorasValidez", 8));
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = config["Jwt:Issuer"],
                Audience = config["Jwt:Audience"],
                Expires = expira,
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.Rol)
                }),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                    SecurityAlgorithms.HmacSha256)
            };
            return (new JsonWebTokenHandler().CreateToken(descriptor), expira);
        }
    }
}