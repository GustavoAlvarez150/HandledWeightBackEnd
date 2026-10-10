using HandledWeight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeviceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("createDevice")]
        public async Task<IActionResult> CreateDevice([FromBody] Entities.Devices device) {
            Console.WriteLine("Received device: " + device.Name);
            if (string.IsNullOrWhiteSpace(device.Unit))
            {
                device.Unit = null;
            }
            device.CreatedAt = DateTime.Now;
            device.UpdatedAt = DateTime.Now;
            await _context.Devices.AddAsync(device);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Device received successfully", device });
        }

        [HttpGet("getScale")]
        public async Task<IActionResult> GetScale() {
            var devices = await _context.Devices
                .Where(d => d.Unit != null && d.Unit != "" && d.IsActive == true)
                .ToListAsync();
            Console.WriteLine(devices);
            return Ok(devices);
        }

        [HttpGet("getPrint")]
        public async Task<IActionResult> GetPrint()
        {
            var devices = await _context.Devices
                .Where(d => (d.Unit == null || d.Unit == "") && d.IsActive == true)
                .ToListAsync();
            return Ok(devices);
        }
    }
}
