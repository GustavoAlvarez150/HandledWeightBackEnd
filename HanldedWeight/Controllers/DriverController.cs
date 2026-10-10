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
    }
}
