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

        [HttpPut("updateBussinesP/{id?}")]
        public async Task<IActionResult> UpdateBussinesP(int? id, [FromBody] Entities.BussinesPartners partner)
        {
            var targetId = (id.HasValue && id.Value > 0) ? id.Value : partner.Id;
            var existing = await _context.BussinesPartners.FindAsync(targetId);
            if (existing == null)
            {
                return NotFound(new { message = "Socio de negocio no encontrado" });
            }

            existing.Name = partner.Name;
            existing.LastNames = partner.LastNames;
            existing.Email = partner.Email;
            existing.Phone = partner.Phone;
            existing.Address = partner.Address;
            existing.City = partner.City;
            existing.State = partner.State;
            existing.Cp = partner.Cp;
            existing.RFC = partner.RFC;
            existing.TaxRegime = partner.TaxRegime;
            existing.IsClient = partner.IsClient;
            if (partner.ProofOfSit != null)
            {
                existing.ProofOfSit = partner.ProofOfSit;
            }
            existing.LastUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Socio de negocio actualizado exitosamente!", partner = existing });
        }
    }
}
