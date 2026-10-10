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

        [HttpPut("updateProduct/{id?}")]
        public async Task<IActionResult> UpdateProduct(int? id, [FromBody] Entities.Products product)
        {
            var targetId = (id.HasValue && id.Value > 0) ? id.Value : product.Id;
            var existing = await _context.Products.FindAsync(targetId);
            if (existing == null)
            {
                return NotFound(new { message = "Producto no encontrado" });
            }

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.Unit = product.Unit;
            existing.LastUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Producto actualizado exitosamente!", product = existing });
        }
    }
}
