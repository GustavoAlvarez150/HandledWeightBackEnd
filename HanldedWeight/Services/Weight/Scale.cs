using HandledWeight.Entities;
using Microsoft.AspNetCore.SignalR;
using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HandledWeight.Services.Weight
{
    public class Scale : IDisposable
    {
        private SerialPort _serialPort;
        private readonly IHubContext<ScaleHub> _hubContext;
        private readonly object _lock = new object();

        public Scale(IHubContext<ScaleHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void IniciarConexion(string puertoCom, Devices propersCom)
        {
            CerrarConexion();

            var parity = Enum.TryParse<Parity>(propersCom.Parity, true, out var parsedParity) ? parsedParity : Parity.None;
            var stopBits = Enum.TryParse<StopBits>(propersCom.StopBits, true, out var parsedStopBits) ? parsedStopBits : StopBits.One;

            _serialPort = new SerialPort(puertoCom, propersCom.BaudRate, parity, propersCom.DataBits, stopBits)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                NewLine = "\r\n" // El M2000 suele terminar en CR LF o CR
            };

            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Open();
        }

        /// <summary>
        /// Envía el comando "P\r\n" al M2000 para solicitar el peso en ese instante.
        /// </summary>
        public void SolicitarPesoBajoDemanda()
        {
            lock (_lock)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    try
                    {
                        // Enviar comando 'P' (Print / Peso) seguido de retorno de carro y nueva línea
                        _serialPort.Write("P\r\n");
                    }
                    catch (Exception ex)
                    {
                        // Manejo de error de escritura (ej. desconexión física)
                        Console.WriteLine($"Error al solicitar peso: {ex.Message}");
                    }
                }
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Leer la respuesta completa enviada por la báscula
                string trama = _serialPort.ReadLine();

                if (string.IsNullOrWhiteSpace(trama)) return;

                decimal? peso = ExtraerPesoDecimal(trama);

                if (peso.HasValue)
                {
                    // Emitir a todos los clientes via SignalR
                    _ = _hubContext.Clients.All.SendAsync("ReceiveWeight", peso.Value);
                }
            }
            catch (TimeoutException)
            {
                // Incompleto o no finalizó la trama a tiempo
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando trama de la báscula: {ex.Message}");
            }
        }

        /// <summary>
        /// Parsea tramas típicas del M2000 como "ST,GS,+001250.0kg", "ST,GS,   125.0 kg" o similar.
        /// </summary>
        private decimal? ExtraerPesoDecimal(string trama)
        {
            // Extrae el número (con signo y decimales) usando Regex
            var match = Regex.Match(trama, @"(?<peso>[-+]?\d+(\.\d+)?)");

            if (match.Success && decimal.TryParse(match.Groups["peso"].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pesoResult))
            {
                return pesoResult;
            }

            return null;
        }

        public void CerrarConexion()
        {
            lock (_lock)
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DataReceived -= OnDataReceived;
                        _serialPort.Close();
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
        }

        public void Dispose()
        {
            CerrarConexion();
        }
    }
}