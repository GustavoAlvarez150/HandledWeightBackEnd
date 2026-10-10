using HandledWeight.Entities;
using HandledWeight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("setVehicle")]
        async public Task<IActionResult> setVehicle([FromBody] Entities.Vehicle vehicle)
        {
            vehicle.CreatedAt = DateTime.Now;
            vehicle.LastUpdatedAt = DateTime.Now;
            await _context.Vehicle.AddAsync(vehicle);
            await _context.SaveChangesAsync();
            return Ok(new { message = "vehicle received successfully", vehicle });
        }

        [HttpGet("getVehicle")]
        async public Task<IActionResult> getVehicle()
        {
            var vehicles = _context.Vehicle.ToList();
            return Ok(vehicles);
        }
    }
}
