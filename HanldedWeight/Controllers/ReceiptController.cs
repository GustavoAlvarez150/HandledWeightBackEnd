using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HandledWeight.Infrastructure.Persistence;
using HandledWeight.Entities;
using HandledWeight.Services;
using HandledWeight.Interfaces;

namespace HandledWeight.Controllers
{
    public class SetWeightOutDto
    {
        public int? Id { get; set; }
        public decimal? WeightOut { get; set; }
        public decimal? Tara { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IComPrintInterface _printerService;

        public ReceiptController(AppDbContext db, IComPrintInterface printerService)
        {
            _db = db;
            _printerService = printerService;
        }

        // POST: api/Receipt
        [HttpPost]
        public async Task<ActionResult<Receipts>> PostReceipt([FromBody] Receipts receipt)
        {
            if (receipt == null)
                return BadRequest(new { message = "El objeto de recibo no puede ser nulo." });

            // Verificar si el vehiculo existe
            var vehicle = await _db.Vehicle.FindAsync(receipt.VehicleId);
            if (vehicle == null)
                return BadRequest(new { message = $"Vehiculo con Id {receipt.VehicleId} no encontrado." });

            // Si no se asigno folio, usar la placa del vehiculo
            if (string.IsNullOrWhiteSpace(receipt.ReceiptNumber))
            {
                receipt.ReceiptNumber = vehicle.Plate ?? string.Empty;
            }

            receipt.CreatedAt = DateTime.UtcNow;
            receipt.LastUpdatedAt = DateTime.UtcNow;

            _db.Receipts.Add(receipt);
            await _db.SaveChangesAsync();

            // Cargar relaciones para devolver el objeto completo
            await _db.Entry(receipt).Reference(r => r.Product).LoadAsync();
            await _db.Entry(receipt).Reference(r => r.Vehicle).LoadAsync();
            await _db.Entry(receipt).Reference(r => r.Driver).LoadAsync();
            await _db.Entry(receipt).Reference(r => r.BussinesPartner).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
        }

        // GET: api/Receipt/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Receipts>> GetById(int id)
        {
            var receipt = await _db.Receipts
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (receipt == null) 
                return NotFound(new { message = $"Recibo con Id {id} no encontrado." });

            return Ok(receipt);
        }

        // GET: api/Receipt/pending
        // Retorna recibos que tienen WeightOut en 0 o nulo (salida pendiente)
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<Receipts>>> GetPending()
        {
            var pending = await _db.Receipts
                .Where(r => (r.WeightOut == null || r.WeightOut == 0) && r.DeletedAt == null)
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(pending);
        }

        // PATCH: api/Receipt/SetWeightOut/{id}
        // Recibe el id en la ruta (o en el body) y actualiza WeightOut/Tara, recalcula totales e imprime ticket.
        [HttpPatch("SetWeightOut/{id?}")]
        [HttpPost("SetWeightOut/{id?}")]
        public async Task<ActionResult<Receipts>> SetWeightOut([FromRoute] int? id, [FromBody] SetWeightOutDto? dto = null)
        {
            var targetId = (id.HasValue && id.Value > 0) ? id.Value : (dto?.Id ?? 0);
            if (targetId <= 0)
                return BadRequest(new { message = "Se requiere un ID de recibo valido." });

            var receipt = await _db.Receipts
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .FirstOrDefaultAsync(r => r.Id == targetId && r.DeletedAt == null);

            if (receipt == null)
                return NotFound(new { message = $"Recibo con Id {targetId} no encontrado." });

            // Asignar peso de salida (tara) si se recibio en el payload
            if (dto?.WeightOut.HasValue == true && dto.WeightOut.Value > 0)
            {
                receipt.WeightOut = dto.WeightOut.Value;
                receipt.Tara = dto.WeightOut.Value;
            }
            else if (dto?.Tara.HasValue == true && dto.Tara.Value > 0)
            {
                receipt.WeightOut = dto.Tara.Value;
                receipt.Tara = dto.Tara.Value;
            }
            else if (!receipt.WeightOut.HasValue || receipt.WeightOut == 0)
            {
                receipt.WeightOut = receipt.Tara ?? receipt.GrossWeight ?? receipt.WeightIn ?? 0;
            }

            // Asegurar GrossWeight si no venia asignado pero si WeightIn
            if ((!receipt.GrossWeight.HasValue || receipt.GrossWeight == 0) && receipt.WeightIn.HasValue)
            {
                receipt.GrossWeight = receipt.WeightIn;
            }

            // Recalcular Peso Neto
            if (receipt.GrossWeight.HasValue && receipt.Tara.HasValue && receipt.Tara > 0)
            {
                receipt.TotalWeight = Math.Abs(receipt.GrossWeight.Value - receipt.Tara.Value);
            }
            else if (receipt.WeightIn.HasValue && receipt.WeightOut.HasValue)
            {
                receipt.TotalWeight = Math.Abs(receipt.WeightIn.Value - receipt.WeightOut.Value);
            }

            // Recalcular Factor de Ajuste (descuento) si aplica
            if (receipt.FitFactor.HasValue && receipt.FitFactor > 0 && receipt.TotalWeight.HasValue)
            {
                decimal factorDec = receipt.FitFactor.Value >= 1 ? (receipt.FitFactor.Value / 100m) : receipt.FitFactor.Value;
                receipt.TotalWeightAdjust = receipt.TotalWeight.Value * (1m - factorDec);
            }
            else if (receipt.TotalWeight.HasValue)
            {
                receipt.TotalWeightAdjust = receipt.TotalWeight;
            }

            // Recalcular Importe Total si aplica precio
            if (receipt.Price.HasValue && receipt.Price > 0 && receipt.TotalWeightAdjust.HasValue)
            {
                receipt.TotalAmount = (receipt.TotalWeightAdjust.Value) * (receipt.Price.Value);
            }
            else if (receipt.Price.HasValue && receipt.Price > 0 && receipt.TotalWeight.HasValue)
            {
                receipt.TotalAmount = (receipt.TotalWeight.Value) * (receipt.Price.Value);
            }

            receipt.LastUpdatedAt = DateTime.UtcNow;

            _db.Receipts.Update(receipt);
            await _db.SaveChangesAsync();

            // Buscar impresora activa y enviar a imprimir el ticket
            try
            {
                var device = await _db.Devices.FirstOrDefaultAsync(d => d.IsActive && d.Unit == null)
                             ?? await _db.Devices.FirstOrDefaultAsync(d => d.IsActive);

                if (device != null)
                {
                    var pageToPrint = PosTicketService.GenerateReceiptText(receipt, useEscPos: true);
                    await _printerService.PrintTextAsync(device.Port, pageToPrint, device.BaudRate);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al imprimir recibo #{receipt.Id}: {ex.Message}");
            }

            return Ok(receipt);
        }

        // POST: api/Receipt/print/{id}
        // Endpoint para imprimir o reimprimir un recibo existente
        [HttpPost("print/{id}")]
        [HttpGet("print/{id}")]
        public async Task<IActionResult> PrintReceipt(int id)
        {
            var receipt = await _db.Receipts
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (receipt == null)
                return NotFound(new { message = $"Recibo con Id {id} no encontrado." });

            var device = await _db.Devices.FirstOrDefaultAsync(d => d.IsActive && d.Unit == null)
                         ?? await _db.Devices.FirstOrDefaultAsync(d => d.IsActive);

            if (device == null)
                return BadRequest(new { message = "No hay impresora activa configurada." });

            var pageToPrint = PosTicketService.GenerateReceiptText(receipt, useEscPos: true);
            var success = await _printerService.PrintTextAsync(device.Port, pageToPrint, device.BaudRate);

            return Ok(new 
            { 
                message = success ? "Ticket enviado a la impresora correctamente" : "No se pudo comunicar con el puerto de la impresora", 
                success, 
                port = device.Port 
            });
        }
    }
}
