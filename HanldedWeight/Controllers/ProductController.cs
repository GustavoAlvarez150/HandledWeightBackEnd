using HandledWeight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }


        [HttpPost("setProduct")]
        async public Task<IActionResult> setProduct([FromBody] Entities.Products products)
        {
            products.CreatedAt = DateTime.Now;
            products.LastUpdatedAt = DateTime.Now;
            _context.Products.Add(products);
            await _context.SaveChangesAsync();
            return Ok("Registro exitoso");
        }

        [HttpGet("getProduct")]
        async public Task<IActionResult> getProduct()
        {
            var products = _context.Products.Where(d => d.DeletedAt == null).ToList();
            return Ok(products);
        }

    }
}
