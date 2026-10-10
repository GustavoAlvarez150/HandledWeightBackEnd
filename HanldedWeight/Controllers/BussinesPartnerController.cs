using HandledWeight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BussinesPartnerController : ControllerBase
    {
        private readonly AppDbContext _context;
        public BussinesPartnerController(AppDbContext context)
        {
            _context = context;
        }


        [HttpPost("setBussinesP")]
        async public Task<IActionResult> setBussinesP([FromBody] Entities.BussinesPartners bussinesPartners)
        {
            bussinesPartners.CreatedAt = DateTime.Now;
            bussinesPartners.LastUpdatedAt = DateTime.Now;
            bussinesPartners.DeletedAt = null;
            _context.BussinesPartners.Add(bussinesPartners);
            await _context.SaveChangesAsync();
            return Ok("Proveedor registrado!");
        }

        [HttpGet("getBussinesP")]
        async public Task<IActionResult> getBussinesP()
        {
            var bussinesPar = _context.BussinesPartners.Where(d => d.DeletedAt == null).ToList();
            return Ok(bussinesPar);
        }
    }
}
