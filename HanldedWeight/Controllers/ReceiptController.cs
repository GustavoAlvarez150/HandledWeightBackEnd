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
        public int? PrinterId { get; set; }
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

        // GET: api/Receipt
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Receipts>>> GetAll(
            [FromQuery] bool? isPublic = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? plate = null,
            [FromQuery] string? folio = null)
        {
            var query = _db.Receipts.Where(r => r.DeletedAt == null);

            if (isPublic.HasValue)
            {
                query = query.Where(r => r.IsPublic == isPublic.Value);
            }

            if (startDate.HasValue)
            {
                var sDate = startDate.Value.Date;
                query = query.Where(r => r.CreatedAt >= sDate);
            }

            if (endDate.HasValue)
            {
                var eDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.CreatedAt <= eDate);
            }

            if (!string.IsNullOrWhiteSpace(plate))
            {
                var pClean = plate.Trim().ToLower();
                query = query.Where(r => r.Vehicle != null && r.Vehicle.Plate.ToLower().Contains(pClean));
            }

            if (!string.IsNullOrWhiteSpace(folio))
            {
                var fClean = folio.Trim().ToLower();
                query = query.Where(r => r.ReceiptNumber != null && r.ReceiptNumber.ToLower().Contains(fClean));
            }

            var list = await query
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(list);
        }

        // POST: api/Receipt
        [HttpPost]
        public async Task<ActionResult<Receipts>> PostReceipt([FromBody] Receipts receipt)
        {
            if (receipt == null)
                return BadRequest(new { message = "El objeto de recibo no puede ser nulo." });

            try
            {
                // Verificar si el vehiculo existe
                var vehicle = await _db.Vehicle.FindAsync(receipt.VehicleId);
                if (vehicle == null)
                    return BadRequest(new { message = $"Vehiculo con Id {receipt.VehicleId} no encontrado." });

                // Asignar conductor por defecto si no es valido
                if (receipt.DriverId <= 0 || await _db.Drivers.FindAsync(receipt.DriverId) == null)
                {
                    var defaultDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.DeletedAt == null);
                    if (defaultDriver != null) receipt.DriverId = defaultDriver.Id;
                }

                // Asignar producto por defecto si no viene especificado
                if (receipt.ProductId <= 0 || await _db.Products.FindAsync(receipt.ProductId) == null)
                {
                    var defaultProd = await _db.Products.FirstOrDefaultAsync(p => p.DeletedAt == null);
                    if (defaultProd != null) receipt.ProductId = defaultProd.Id;
                }

                // Asignar socio por defecto si no viene especificado
                if (receipt.BussinesPartnerId <= 0 || await _db.BussinesPartners.FindAsync(receipt.BussinesPartnerId) == null)
                {
                    var defaultBp = await _db.BussinesPartners.FirstOrDefaultAsync(bp => bp.DeletedAt == null);
                    if (defaultBp != null) receipt.BussinesPartnerId = defaultBp.Id;
                }

                // Si es pesada publica, normalizar pesos
                if (receipt.IsPublic)
                {
                    receipt.WeightIn = receipt.WeightIn ?? receipt.GrossWeight ?? 0;
                    receipt.GrossWeight = receipt.WeightIn;
                    receipt.WeightOut = 0;
                    receipt.Tara = 0;
                    receipt.TotalWeight = receipt.WeightIn;
                    receipt.TotalWeightAdjust = receipt.WeightIn;
                }
                else if (!receipt.WeightOut.HasValue || receipt.WeightOut == 0)
                {
                    // Pesada interna pendiente de salida: dejar WeightOut y Tara en null
                    receipt.WeightOut = null;
                    receipt.Tara = null;
                }

                // Si no se asigno folio, usar la placa del vehiculo
                if (string.IsNullOrWhiteSpace(receipt.ReceiptNumber))
                {
                    receipt.ReceiptNumber = receipt.IsPublic 
                        ? $"PUB-{DateTime.Now:yyyyMMddHHmmss}" 
                        : (vehicle.Plate ?? string.Empty);
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
            catch (Exception ex)
            {
                var innermost = ex;
                while (innermost.InnerException != null) innermost = innermost.InnerException;
                return StatusCode(500, new { message = $"Error al registrar recibo: {innermost.Message}" });
            }
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
        // Retorna recibos internos que tienen WeightOut nulo (salida pendiente)
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<Receipts>>> GetPending()
        {
            var pending = await _db.Receipts
                .Where(r => r.WeightOut == null && !r.IsPublic && r.DeletedAt == null)
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(pending);
        }

        // POST/PATCH: api/Receipt/Finalize/{id}
        // Finaliza un recibo pendiente con una sola pesada registrada (WeightIn)
        [HttpPost("Finalize/{id}")]
        [HttpPatch("Finalize/{id}")]
        public async Task<ActionResult<Receipts>> FinalizeSingleWeight(int id)
        {
            var receipt = await _db.Receipts
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (receipt == null)
                return NotFound(new { message = $"Recibo con Id {id} no encontrado." });

            decimal pEntrada = receipt.WeightIn ?? receipt.GrossWeight ?? 0;
            receipt.WeightIn = pEntrada;
            receipt.GrossWeight = pEntrada;
            receipt.WeightOut = 0;
            receipt.Tara = 0;
            receipt.TotalWeight = pEntrada;

            if (receipt.FitFactor.HasValue && receipt.FitFactor > 0)
            {
                decimal factorDec = receipt.FitFactor.Value >= 1 ? (receipt.FitFactor.Value / 100m) : receipt.FitFactor.Value;
                receipt.TotalWeightAdjust = pEntrada * (1m - factorDec);
            }
            else
            {
                receipt.TotalWeightAdjust = pEntrada;
            }

            if (receipt.Price.HasValue && receipt.Price > 0)
            {
                receipt.TotalAmount = (receipt.TotalWeightAdjust ?? pEntrada) * receipt.Price.Value;
            }

            receipt.LastUpdatedAt = DateTime.UtcNow;

            _db.Receipts.Update(receipt);
            await _db.SaveChangesAsync();

            return Ok(receipt);
        }

        // PATCH: api/Receipt/SetWeightOut/{id}
        // Recibe el id en la ruta (o en el body) y actualiza WeightOut/Tara, recalcula totales.
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

            // Recalcular Peso Neto: si entrada es mayor que salida (WeightIn - WeightOut), si salida es mayor (WeightOut - WeightIn)
            decimal pEntrada = receipt.WeightIn ?? receipt.GrossWeight ?? 0;
            decimal pSalida = receipt.WeightOut ?? receipt.Tara ?? 0;
            decimal pNeto = (pEntrada >= pSalida) ? (pEntrada - pSalida) : (pSalida - pEntrada);

            receipt.TotalWeight = pNeto;

            if (pEntrada >= pSalida)
            {
                receipt.GrossWeight = pEntrada;
                receipt.Tara = pSalida;
            }
            else
            {
                receipt.GrossWeight = pSalida;
                receipt.Tara = pEntrada;
            }

            // Recalcular Factor de Ajuste (descuento) si aplica
            if (receipt.FitFactor.HasValue && receipt.FitFactor > 0)
            {
                decimal factorDec = receipt.FitFactor.Value >= 1 ? (receipt.FitFactor.Value / 100m) : receipt.FitFactor.Value;
                receipt.TotalWeightAdjust = pNeto * (1m - factorDec);
            }
            else
            {
                receipt.TotalWeightAdjust = pNeto;
            }

            // Recalcular Importe Total si aplica precio
            if (receipt.Price.HasValue && receipt.Price > 0)
            {
                receipt.TotalAmount = (receipt.TotalWeightAdjust ?? pNeto) * (receipt.Price.Value);
            }

            receipt.LastUpdatedAt = DateTime.UtcNow;

            _db.Receipts.Update(receipt);
            await _db.SaveChangesAsync();

            return Ok(receipt);
        }

        // POST: api/Receipt/print/{id}
        // Endpoint para imprimir o reimprimir un recibo existente
        [HttpPost("print/{id}")]
        [HttpGet("print/{id}")]
        public async Task<IActionResult> PrintReceipt(int id, [FromQuery] int? printerId = null)
        {
            var receipt = await _db.Receipts
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (receipt == null)
                return NotFound(new { message = $"Recibo con Id {id} no encontrado." });

            Devices? device = null;
            if (printerId.HasValue && printerId.Value > 0)
            {
                device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == printerId.Value && d.DeletedAt == null);
            }

            if (device == null || string.IsNullOrWhiteSpace(device.Port))
            {
                device = await _db.Devices
                    .Where(d => d.IsActive && (d.Unit == null || d.Unit.Trim() == "") && !string.IsNullOrWhiteSpace(d.Port))
                    .OrderByDescending(d => d.UpdatedAt)
                    .FirstOrDefaultAsync();
            }

            if (device == null || string.IsNullOrWhiteSpace(device.Port))
                return BadRequest(new { message = "No hay impresora activa con puerto COM valido configurada." });

            var factory = await _db.Factory.Where(f => f.DeletedAt == null).OrderByDescending(f => f.Id).FirstOrDefaultAsync();
            var pageToPrint = PosTicketService.GenerateReceiptText(receipt, factory, useEscPos: false);
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

