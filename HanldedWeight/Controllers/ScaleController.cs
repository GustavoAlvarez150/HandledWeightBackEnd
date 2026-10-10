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
            var scale = await _context.Devices.FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
            if (scale == null)
            {
                //Abrimos la conexion hacia la báscula
                return Ok("No encontro nada alv");
            }
                this.scaleService.IniciarConexion(scale.Port, scale);
                this.scaleService.SolicitarPesoBajoDemanda();
            // Implementation needed here. For now, return Ok to satisfy return requirements.
            return Ok("Creo que jalo esta madre");
        }
    }
}
