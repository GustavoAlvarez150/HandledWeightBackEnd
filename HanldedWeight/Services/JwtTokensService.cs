using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HandledWeight.Services
{
    public class JwtTokensService
    {
        private readonly IConfiguration _config;

        public JwtTokensService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(DTOs.Auth.User user)
        {
            // 1. Definir los Claims (información que va dentro del Payload)
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.name.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.email),
            new Claim("idRole", user.idRole.ToString() ?? 0 + ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ID único del token
        };
            
            // 2. Obtener la clave secreta desde appsettings.json
            var secretKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // 3. Definir las credenciales de firma (Algoritmo HMAC SHA256)
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 4. Crear el token con sus propiedades
            var expirationInMinutes = double.Parse(_config["JwtSettings:ExpirationInMinutes"] ?? "120");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes), // Expiración en 1 hora
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // 5. Retornar el token serializado en formato string Base64
            return tokenHandler.WriteToken(token);
        }
    }
}
