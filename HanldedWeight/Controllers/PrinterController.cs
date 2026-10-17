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

            // 1. Inicializar impresora
            sb.Append("\x1B@");          // ESC @ : Reset / Inicializar

            // 2. Establecer p�gina de caracteres ESC/POS a PC850 (Multilingual) o PC437
            // \x1B\x74\x02 = ESC t 2 (P�gina 850 Multiling�e - com�n en t�rmicas EPSON / Bematech / gen�ricas)
            sb.Append("\x1B\x74\x02");

            // Encabezado
            sb.Append("\x1Ba\x01");      // ESC a 1 : Centrar
            sb.AppendLine("FORRAJERA SAN RAFAEL, S.A. DE C.V.");
            sb.AppendLine("KM.4 CARR. SAN JUAN - GUADALAJARA");
            sb.AppendLine("SAN JUAN DE LOS LAGOS, JAL.");
            sb.AppendLine("TEL: (395) 785 33 06");
            sb.AppendLine();

            // T�tulo destacado
            sb.Append("\x1BE\x01");      // ESC E 1 : Negrita ON
            sb.AppendLine("PRUEBA DE CONEXION");
            sb.Append("\x1BE\x00");      // ESC E 0 : Negrita OFF
            sb.AppendLine(new string('-', 40));

            // Datos del dispositivo
            sb.Append("\x1Ba\x00");      // ESC a 0 : Alineaci�n izquierda
            sb.AppendLine($"Fecha/Hora:  {DateTime.Now:dd/MM/yyyy hh:mm:ss tt}");
            sb.AppendLine($"Dispositivo: {printer.Name}");
            sb.AppendLine($"Puerto COM:  {printer.Port}");
            sb.AppendLine($"BaudRate:    {printer.BaudRate}");
            sb.AppendLine("Estado:      Activa y Configurada");
            sb.AppendLine(new string('-', 40));
            sb.AppendLine();

            // Pie
            sb.Append("\x1Ba\x01");      // Centrar
            sb.AppendLine("Impresora lista para operar.");
            sb.AppendLine("WWW.GRUPOSANRAFAEL.COM.MX");

            // 3. Alimentar papel antes del corte (evita que el corte caiga sobre el texto)
            sb.Append("\x1Bd\x04");      // ESC d 4 : Avanza 4 l�neas
            sb.Append("\x1DV\x41\x03");  // GS V A 3 : Corte parcial con margen

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
