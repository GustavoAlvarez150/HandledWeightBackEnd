using HandledWeight.Infrastructure.Persistence;
using HandledWeight.Services.Weight;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScaleController : ControllerBase
    {
        private readonly AppDbContext _context;
        public readonly Scale scaleService;

        public ScaleController(AppDbContext context, Scale scaleService)
        {
            _context = context;
            this.scaleService = scaleService;
        }

        [HttpGet("SetScale/{id}")]
        public async Task<IActionResult> SetScale(int id)
        {
            var scale = await _context.Devices.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);
            if (scale == null)
            {
                return NotFound(new { message = "Báscula no encontrada." });
            }

            scale.IsActive = true;
            scale.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            bool connected = false;
            string connMsg = "Báscula establecida como predeterminada y conectada.";
            try
            {
                if (!string.IsNullOrWhiteSpace(scale.Port))
                {
                    this.scaleService.IniciarConexion(scale.Port, scale);
                    this.scaleService.SolicitarPesoBajoDemanda();
                    connected = this.scaleService.IsConnected;
                }
            }
            catch (Exception ex)
            {
                connected = false;
                connMsg = $"Báscula establecida como predeterminada (Puerto {scale.Port}: {ex.Message})";
            }

            return Ok(new
            {
                message = connMsg,
                scale,
                isConnected = connected
            });
        }
    }
}
