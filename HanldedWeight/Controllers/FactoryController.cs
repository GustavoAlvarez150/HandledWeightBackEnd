using System;
using System.Linq;
using System.Threading.Tasks;
using HandledWeight.Entities;
using HandledWeight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FactoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FactoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Factory
        // GET: api/Factory/getFactory
        [HttpGet]
        [HttpGet("getFactory")]
        public async Task<IActionResult> GetFactory()
        {
            var factory = await _context.Factory
                .Where(f => f.DeletedAt == null)
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            if (factory == null)
            {
                return Ok(new Factory
                {
                    Name = "",
                    RFC = "",
                    Address = "",
                    Email = "",
                    Phone = "",
                    Fax = ""
                });
            }

            return Ok(factory);
        }

        // POST: api/Factory
        // POST: api/Factory/setFactory
        // Permite guardar y actualizar con el mismo endpoint y botón
        [HttpPost]
        [HttpPost("setFactory")]
        [HttpPut]
        public async Task<IActionResult> SaveOrUpdate([FromBody] Factory input)
        {
            if (input == null)
            {
                return BadRequest(new { message = "Los datos de la empresa no pueden ser nulos." });
            }

            if (string.IsNullOrWhiteSpace(input.Name))
            {
                return BadRequest(new { message = "El nombre o razón social de la empresa es obligatorio." });
            }

            Factory? existing = null;
            if (input.Id > 0)
            {
                existing = await _context.Factory.FindAsync(input.Id);
            }

            if (existing == null)
            {
                // Si no se pasó Id válido, verificar si ya existe algún registro en Factory
                existing = await _context.Factory
                    .Where(f => f.DeletedAt == null)
                    .OrderByDescending(f => f.Id)
                    .FirstOrDefaultAsync();
            }

            if (existing != null)
            {
                // Actualizar registro existente
                existing.Name = input.Name.Trim();
                existing.RFC = input.RFC?.Trim();
                existing.Address = input.Address?.Trim();
                existing.Email = input.Email?.Trim();
                existing.Phone = input.Phone?.Trim();
                existing.Fax = input.Fax?.Trim();
                existing.LastUpdatedAt = DateTime.UtcNow;

                _context.Factory.Update(existing);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Datos de la empresa actualizados exitosamente.", factory = existing });
            }
            else
            {
                // Crear nuevo registro
                input.Name = input.Name.Trim();
                input.RFC = input.RFC?.Trim();
                input.Address = input.Address?.Trim();
                input.Email = input.Email?.Trim();
                input.Phone = input.Phone?.Trim();
                input.Fax = input.Fax?.Trim();
                input.CreatedAt = DateTime.UtcNow;
                input.LastUpdatedAt = DateTime.UtcNow;

                _context.Factory.Add(input);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Empresa registrada exitosamente.", factory = input });
            }
        }
    }
}

