using HandledWeight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriverController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        public DriverController(AppDbContext context)
        {
            _dbContext = context;
        }

        [HttpPost("setDriver")]
        async public Task<IActionResult> SetDriver([FromBody] Entities.Drivers drivers)
        {
            drivers.CreatedAt = DateTime.Now;
            drivers.LastUpdatedAt = DateTime.Now;
            _dbContext.Drivers.Add(drivers);
            await _dbContext.SaveChangesAsync();
            return Ok("Registro extitoso!");
        }


        [HttpGet("getDriver")]
        async public Task<IActionResult> getDriver()
        {
            var drivers =_dbContext.Drivers.Where(d => d.DeletedAt == null).ToList();
            return Ok(drivers);
        }

        [HttpPut("updateDriver/{id?}")]
        public async Task<IActionResult> UpdateDriver(int? id, [FromBody] Entities.Drivers driver)
        {
            var targetId = (id.HasValue && id.Value > 0) ? id.Value : driver.Id;
            var existing = await _dbContext.Drivers.FindAsync(targetId);
            if (existing == null)
            {
                return NotFound(new { message = "Conductor no encontrado" });
            }

            existing.Name = driver.Name;
            existing.LicenseNumber = driver.LicenseNumber;
            existing.Address = driver.Address;
            existing.State = driver.State;
            existing.city = driver.city;
            existing.Phone = driver.Phone;
            existing.Phone1 = driver.Phone1;
            existing.LastUpdatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Conductor actualizado exitosamente!", driver = existing });
        }
    }
}
