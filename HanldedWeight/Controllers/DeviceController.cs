using HandledWeight.Infrastructure.Persistence;
using HandledWeight.Interfaces;
using HandledWeight.Services.Weight;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandledWeight.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Scale _scaleService;
        private readonly IComPrintInterface _printerService;

        public DeviceController(AppDbContext context, Scale scaleService, IComPrintInterface printerService)
        {
            _context = context;
            _scaleService = scaleService;
            _printerService = printerService;
        }

        [HttpPost("createDevice")]
        public async Task<IActionResult> CreateDevice([FromBody] Entities.Devices device) {
            Console.WriteLine("Received device: " + device.Name);
            if (string.IsNullOrWhiteSpace(device.Unit))
            {
                device.Unit = null;
            }
            device.IsActive = true;
            device.CreatedAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;
            await _context.Devices.AddAsync(device);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Device received successfully", device });
        }

        [HttpGet("getScale")]
        public async Task<IActionResult> GetScale() {
            var devices = await _context.Devices
                .Where(d => d.IsActive == true && d.DeletedAt == null && d.Unit != null && d.Unit != "" && d.Unit.Trim() != "")
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();
            return Ok(devices);
        }

        [HttpGet("getPrint")]
        public async Task<IActionResult> GetPrint()
        {
            var devices = await _context.Devices
                .Where(d => d.IsActive == true && d.DeletedAt == null && (d.Unit == null || d.Unit == "" || d.Unit.Trim() == ""))
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();
            return Ok(devices);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus([FromQuery] int? scaleId = null, [FromQuery] int? printerId = null)
        {
            Entities.Devices? defaultScale = null;
            if (scaleId.HasValue && scaleId.Value > 0)
            {
                defaultScale = await _context.Devices.FirstOrDefaultAsync(d => d.Id == scaleId.Value && d.DeletedAt == null);
            }
            if (defaultScale == null)
            {
                defaultScale = await _context.Devices
                    .Where(d => d.IsActive == true && d.DeletedAt == null && d.Unit != null && d.Unit.Trim() != "")
                    .OrderByDescending(d => d.UpdatedAt)
                    .FirstOrDefaultAsync();
            }

            Entities.Devices? defaultPrinter = null;
            if (printerId.HasValue && printerId.Value > 0)
            {
                defaultPrinter = await _context.Devices.FirstOrDefaultAsync(d => d.Id == printerId.Value && d.DeletedAt == null);
            }
            if (defaultPrinter == null)
            {
                defaultPrinter = await _context.Devices
                    .Where(d => d.IsActive == true && d.DeletedAt == null && (d.Unit == null || d.Unit.Trim() == ""))
                    .OrderByDescending(d => d.UpdatedAt)
                    .FirstOrDefaultAsync();
            }

            // Si la bascula predeterminada existe y aun no se ha intentado conectar en esta sesion, intentar abrirla
            if (defaultScale != null && _scaleService.ActiveDevice == null && !string.IsNullOrWhiteSpace(defaultScale.Port))
            {
                try
                {
                    _scaleService.IniciarConexion(defaultScale.Port, defaultScale);
                }
                catch
                {
                    // Ignorar si el puerto COM fisico no esta disponible
                }
            }

            var availablePorts = _printerService.GetAvailablePorts() ?? Array.Empty<string>();
            bool scalePortAvailable = defaultScale != null && !string.IsNullOrWhiteSpace(defaultScale.Port) &&
                availablePorts.Any(p => string.Equals(p, defaultScale.Port, StringComparison.OrdinalIgnoreCase));
            bool scaleConnected = _scaleService.IsConnected || scalePortAvailable;

            bool printerPortAvailable = defaultPrinter != null && !string.IsNullOrWhiteSpace(defaultPrinter.Port) &&
                availablePorts.Any(p => string.Equals(p, defaultPrinter.Port, StringComparison.OrdinalIgnoreCase));
            bool printerConfigured = defaultPrinter != null && !string.IsNullOrWhiteSpace(defaultPrinter.Port);

            return Ok(new
            {
                scale = _scaleService.ActiveDevice ?? defaultScale,
                scaleConnected,
                scaleSerialOpen = _scaleService.IsConnected,
                scaleConfigured = defaultScale != null && !string.IsNullOrWhiteSpace(defaultScale.Port),
                printer = defaultPrinter,
                printerConnected = printerPortAvailable || printerConfigured,
                printerPortAvailable,
                printerConfigured,
                availablePorts
            });
        }
    }
}
