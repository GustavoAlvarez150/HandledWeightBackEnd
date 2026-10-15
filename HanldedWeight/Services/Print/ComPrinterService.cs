using System.IO.Ports;
using System.Text;
using HandledWeight.Entities;
using HandledWeight.Interfaces;
using HandledWeight.Services;
using Microsoft.Extensions.Logging;
namespace HandledWeight.Services.Print
{
    public class ComPrinterService : IComPrintInterface
    {
        private readonly ILogger<ComPrinterService> _logger;

        public ComPrinterService(ILogger<ComPrinterService> logger)
        {
            _logger = logger;
        }

        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        public string GenerateReceiptText(Receipts receipt, bool useEscPos = true)
        {
            return PosTicketService.GenerateReceiptText(receipt, useEscPos);
        }

        public async Task<bool> PrintTextAsync(string portName, string text, int baudRate = 9600)
        {
            // Sanitizar a ASCII puro de 1 byte para compatibilidad total con impresoras de matriz de puntos (POS)
            string cleanText = PosTicketService.SanitizeToAscii(text);
            byte[] textBytes = Encoding.ASCII.GetBytes(cleanText);
            return await SendBytesAsync(portName, textBytes, baudRate);
        }

        public async Task<bool> SendBytesAsync(string portName, byte[] bytes, int baudRate = 9600)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One))
                    {
                        serialPort.DtrEnable = true;
                        serialPort.RtsEnable = true;
                        serialPort.WriteTimeout = 5000;
                        serialPort.Open();

                        if (serialPort.IsOpen)
                        {
                            serialPort.Write(bytes, 0, bytes.Length);
                            _logger.LogInformation("Datos enviados exitosamente a {PortName}", portName);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al imprimir en el puerto {PortName}", portName);
                }

                return false;
            });
        }
    }
}
