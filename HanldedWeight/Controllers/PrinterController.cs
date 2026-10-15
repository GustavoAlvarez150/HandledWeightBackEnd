using HandledWeight.Entities;
using HandledWeight.Infrastructure.Persistence;
using HandledWeight.Interfaces;
using HandledWeight.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrinterController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IComPrintInterface _printerService;

        public PrinterController(AppDbContext context, IComPrintInterface printer) 
        { 
            this._context = context;
            this._printerService = printer;
        }

        [HttpGet("SetPrinter/{id}")]
        public async Task<IActionResult> SetPrinter(int id)
        {
            var printer = await _context.Devices.FirstOrDefaultAsync(s => s.Id == id);
            if (printer == null)
            {
                return NotFound(new { message = "No se encontro impresora." });
            }

            // Marcar esta impresora como predeterminada actualizando su fecha
            printer.IsActive = true;
            printer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var sb = new StringBuilder();
            sb.AppendLine(PosTicketService.CenterLine("FORRAJERA SAN RAFAEL, S.A. DE C.V."));
            sb.AppendLine(PosTicketService.CenterLine("KM.4 CARR. SAN JUAN - GUADALAJARA"));
            sb.AppendLine(PosTicketService.CenterLine("SAN JUAN DE LOS LAGOS, JAL."));
            sb.AppendLine(PosTicketService.CenterLine("TEL: (395) 785 33 06"));
            sb.AppendLine();
            sb.AppendLine(PosTicketService.CenterLine("*** PRUEBA DE CONEXION ***"));
            sb.AppendLine(new string('=', 40));
            sb.AppendLine(PosTicketService.FormatLine("Fecha/Hora:", DateTime.Now.ToString("dd/MM/yyyy hh:mm tt")));
            sb.AppendLine(PosTicketService.FormatLine("Dispositivo:", printer.Name ?? "Impresora"));
            sb.AppendLine(PosTicketService.FormatLine("Puerto COM:", printer.Port ?? "N/A"));
            sb.AppendLine(PosTicketService.FormatLine("BaudRate:", printer.BaudRate.ToString()));
            sb.AppendLine(PosTicketService.FormatLine("Estado:", "Activa y Configurada"));
            sb.AppendLine(new string('=', 40));
            sb.AppendLine();
            sb.AppendLine(PosTicketService.CenterLine("Impresora lista para operar."));
            sb.AppendLine(PosTicketService.CenterLine("WWW.GRUPOSANRAFAEL.COM.MX"));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();

            var pageToPrint = sb.ToString();

            bool printSuccess = false;
            try
            {
                printSuccess = await this._printerService.PrintTextAsync(printer.Port, pageToPrint, printer.BaudRate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al enviar prueba a la impresora: {ex.Message}", printer });
            }

            return Ok(new 
            { 
                message = printSuccess ? "Impresora seleccionada y probada correctamente" : "Impresora guardada, pero no respondio al puerto COM", 
                printer,
                success = printSuccess 
            });
        }

        [HttpGet("PrintReceipt/{receiptId}")]
        [HttpPost("PrintReceipt/{receiptId}")]
        public async Task<IActionResult> PrintReceipt(int receiptId, [FromQuery] int? printerId = null)
        {
            var receipt = await _context.Receipts
                .Include(r => r.Product)
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Include(r => r.BussinesPartner)
                .FirstOrDefaultAsync(r => r.Id == receiptId && r.DeletedAt == null);

            if (receipt == null)
            {
                return NotFound(new { message = $"No se encontro el recibo con ID {receiptId}." });
            }

            Devices? device = null;
            if (printerId.HasValue && printerId.Value > 0)
            {
                device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == printerId.Value && d.DeletedAt == null);
            }

            if (device == null || string.IsNullOrWhiteSpace(device.Port))
            {
                device = await _context.Devices
                    .Where(d => d.IsActive && (d.Unit == null || d.Unit.Trim() == "") && !string.IsNullOrWhiteSpace(d.Port))
                    .OrderByDescending(d => d.UpdatedAt)
                    .FirstOrDefaultAsync();
            }

            if (device == null || string.IsNullOrWhiteSpace(device.Port))
            {
                return BadRequest(new { message = "No hay ninguna impresora activa con puerto COM valido configurada." });
            }

            var factory = await _context.Factory.Where(f => f.DeletedAt == null).OrderByDescending(f => f.Id).FirstOrDefaultAsync();
            var ticketText = PosTicketService.GenerateReceiptText(receipt, factory, useEscPos: false);
            var success = await _printerService.PrintTextAsync(device.Port, ticketText, device.BaudRate);

            return Ok(new 
            { 
                message = success ? "Ticket enviado a la impresora exitosamente." : "Error al enviar ticket a la impresora.", 
                success, 
                port = device.Port,
                receiptId = receipt.Id
            });
        }

        [HttpGet("GetAvailablePorts")]
        public IActionResult GetAvailablePorts()
        {
            var ports = _printerService.GetAvailablePorts();
            return Ok(ports);
        }
    }
}
