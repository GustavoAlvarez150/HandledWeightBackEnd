using HandledWeight.DTOs.Auth;
using HandledWeight.Infrastructure.Persistence;
using HandledWeight.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using System.Threading.Tasks;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtTokensService _jwtTokenService;

        public LoginController(AppDbContext context)
        {
            _context = context;
            _jwtTokenService = new JwtTokensService(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build());
        }


        [HttpPost("Index")]
        public async Task<IActionResult> Index([FromBody] DTOs.Auth.LoginDto login)
        {
            var user = await _context.Users.FirstOrDefaultAsync( u => u.Email == login.email);

            if(user != null)
            {
                bool hashesMatch = BCrypt.Net.BCrypt.Verify(login.password, user.Password);
             
                if (hashesMatch)
                {
                    var userDto = new DTOs.Auth.User(user.Name, user.Email, user.IdRole ?? 0);
                    string tokenReal = _jwtTokenService.GenerateToken(userDto);

                    DTOs.Auth.JWT response = new DTOs.Auth.JWT(
                        message: "Inicio de sesión exitoso",
                        tokenType: "Bearer",
                        expiresIn: 3600, // Tiempo de expiración en segundos (1 hora)
                        token: tokenReal, // Aquí deberías generar un token JWT real
                        user: new User(user.Name, user.Email, user.IdRole ?? 0)
                    );

                    return Ok(response);
                }
            }

            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] Entities.Users user)
        {

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.CreatedAt = DateTime.Now;
            user.LastUpdatedAt = DateTime.Now;
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Registro exitoso" });
        }
    }

}