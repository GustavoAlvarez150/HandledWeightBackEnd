using System;
using System.Text;
using HandledWeight.Entities;

namespace HandledWeight.Services
{
    public class PosTicketService
    {
        // Ancho estándar para tickets de 80mm (generalmente 40 a 48 caracteres monospaced)
        private const int MaxWidth = 42; 

        // Comandos ESC/POS
        private static readonly string EscInit      = "\x1B@";       // Reiniciar impresora
        private static readonly string EscCenter    = "\x1Ba\x01";   // Centrar texto
        private static readonly string EscLeft      = "\x1Ba\x00";   // Alinear izquierda
        private static readonly string EscBoldOn    = "\x1BE\x01";   // Negrita ON
        private static readonly string EscBoldOff   = "\x1BE\x00";   // Negrita OFF
        private static readonly string EscCut       = "\x1DV\x41\x03"; // Cortar papel

        public static string GenerateReceiptText(Receipts receipt, bool useEscPos = true)
        {
            return GenerateReceiptText(receipt, null, useEscPos);
        }

        public static string GenerateReceiptText(Receipts receipt, Factory? factory, bool useEscPos = true)
        {
            var sb = new StringBuilder();

            if (useEscPos) sb.Append(EscInit);

            // ================= ENCABEZADO =================
            if (useEscPos) sb.Append(EscCenter);
            if (factory != null && !string.IsNullOrWhiteSpace(factory.Name))
            {
                sb.AppendLine(factory.Name);
                if (!string.IsNullOrWhiteSpace(factory.RFC)) sb.AppendLine($"RFC: {factory.RFC}");
                if (!string.IsNullOrWhiteSpace(factory.Address)) sb.AppendLine(factory.Address);
                if (!string.IsNullOrWhiteSpace(factory.Phone)) sb.AppendLine($"TEL: {factory.Phone}");
            }
            else
            {
                sb.AppendLine("FORRAJERA SAN RAFAEL, S.A. DE C.V.");
                sb.AppendLine("KM.4 CARR. SAN JUAN - GUADALAJARA");
                sb.AppendLine("SAN JUAN DE LOS LAGOS, JAL.");
                sb.AppendLine("TEL: (395) 785 33 06");
            }
            sb.AppendLine();
            
            if (useEscPos) sb.Append(EscBoldOn);
            if (receipt.IsPublic)
            {
                sb.AppendLine("PESAJE PUBLICO");
            }
            else if (receipt.WeightOut.HasValue && receipt.WeightOut > 0)
            {
                sb.AppendLine("TICKET DE SALIDA");
            }
            else
            {
                sb.AppendLine("TICKET DE ENTRADA");
            }
            if (useEscPos) sb.Append(EscBoldOff);

            sb.AppendLine(new string('-', MaxWidth));

            // ================= DATOS GENERALES =================
            if (useEscPos) sb.Append(EscLeft);
            sb.AppendLine(FormatLine("FOLIO:", receipt.ReceiptNumber ?? receipt.Id.ToString()));
            sb.AppendLine(FormatLine("Fecha/Hora:", receipt.CreatedAt.ToString("dd/MM/yyyy hh:mm:ss tt")));
            sb.AppendLine(FormatLine("Proveedor:", receipt.BussinesPartner?.Name ?? "N/A"));
            sb.AppendLine(FormatLine("Placa:", receipt.Vehicle?.Plate ?? "N/A"));
            sb.AppendLine(FormatLine("Vehiculo:", receipt.Vehicle?.Type ?? receipt.Vehicle?.KindOfVehicle ?? "N/A"));
            sb.AppendLine(FormatLine("Chofer:", receipt.Driver?.Name ?? "N/A"));
            sb.AppendLine(FormatLine("Producto:", receipt.Product?.Name ?? "N/A"));

            sb.AppendLine(new string('-', MaxWidth));

            // ================= PESAJES =================
            decimal pEntrada = receipt.WeightIn ?? receipt.GrossWeight ?? 0;
            decimal pSalida = receipt.WeightOut ?? receipt.Tara ?? 0;

            if (receipt.IsPublic)
            {
                if (useEscPos) sb.Append(EscBoldOn);
                sb.AppendLine(FormatLine("PESO REGISTRADO:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("PESO BRUTO:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("PESO NETO:", $"{pEntrada:N0} kg"));
                if (useEscPos) sb.Append(EscBoldOff);
            }
            else if (pSalida == 0)
            {
                // Solo primera pesada (Entrada)
                if (useEscPos) sb.Append(EscBoldOn);
                sb.AppendLine(FormatLine("PESO ENTRADA:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("PESO SALIDA:", "PENDIENTE"));
                sb.AppendLine(FormatLine("PESO BRUTO:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("ESTADO:", "EN PATIO / PENDIENTE"));
                if (useEscPos) sb.Append(EscBoldOff);
            }
            else
            {
                // Lógica de pesajes: si entrada es mayor que salida (WeightIn - WeightOut), si salida es mayor (WeightOut - WeightIn)
                decimal pNeto = (pEntrada >= pSalida) ? (pEntrada - pSalida) : (pSalida - pEntrada);
                decimal pBruto = (pEntrada >= pSalida) ? pEntrada : pSalida;
                decimal pTara = (pEntrada >= pSalida) ? pSalida : pEntrada;

                if (useEscPos) sb.Append(EscBoldOn);
                sb.AppendLine(FormatLine("PESO ENTRADA:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("PESO SALIDA:", $"{pSalida:N0} kg"));
                sb.AppendLine(FormatLine("PESO BRUTO:", $"{pBruto:N0} kg"));
                sb.AppendLine(FormatLine("TARA:", $"{pTara:N0} kg"));
                sb.AppendLine(FormatLine("PESO NETO:", $"{pNeto:N0} kg"));
                if (useEscPos) sb.Append(EscBoldOff);
            }

            // Factores de ajuste (si aplican y no es público)
            if (!receipt.IsPublic && receipt.FitFactor.HasValue && receipt.FitFactor > 0)
            {
                string factorStr = receipt.FitFactor.Value >= 1 
                    ? $"{receipt.FitFactor.Value:N2}%" 
                    : $"{receipt.FitFactor.Value:P2}";
                sb.AppendLine(FormatLine("FACTOR AJUSTE:", factorStr));
                if (useEscPos) sb.Append(EscBoldOn);
                sb.AppendLine(FormatLine("PESO AJUSTADO:", $"{receipt.TotalWeightAdjust ?? 0:N0} kg"));
                if (useEscPos) sb.Append(EscBoldOff);
            }

            // Datos de precio / importe / tarifa
            if (receipt.TotalAmount > 0)
            {
                sb.AppendLine(new string('-', MaxWidth));
                if (receipt.IsPublic)
                {
                    if (useEscPos) sb.Append(EscBoldOn);
                    sb.AppendLine(FormatLine("TARIFA SERVICIO:", $"{receipt.TotalAmount:C2}"));
                    if (useEscPos) sb.Append(EscBoldOff);
                }
                else
                {
                    sb.AppendLine(FormatLine("PRECIO:", $"{receipt.Price ?? 0:C2}"));
                    if (useEscPos) sb.Append(EscBoldOn);
                    sb.AppendLine(FormatLine("IMPORTE TOTAL:", $"{receipt.TotalAmount:C2}"));
                    if (useEscPos) sb.Append(EscBoldOff);
                }
            }

            // Observaciones
            if (!string.IsNullOrWhiteSpace(receipt.Observations))
            {
                sb.AppendLine(new string('-', MaxWidth));
                sb.AppendLine($"Obs: {receipt.Observations}");
            }

            // ================= PIE DE PÁGINA Y FIRMAS =================
            sb.AppendLine();
            sb.AppendLine();
            if (useEscPos) sb.Append(EscCenter);
            sb.AppendLine("________________________________________");
            sb.AppendLine("FIRMA DE CONFORMIDAD");
            sb.AppendLine();
            sb.AppendLine("calidad y nutricion... hecho alimento");
            sb.AppendLine("WWW.GRUPOSANRAFAEL.COM.MX");
            sb.AppendLine();
            sb.AppendLine();

            // Comando de corte
            if (useEscPos) sb.Append(EscCut);

            return sb.ToString();
        }

        // Formatea el texto con justificación a ambos lados (Etiqueta izquierda, Valor derecha)
        private static string FormatLine(string label, string value)
        {
            int availableSpace = MaxWidth - label.Length;
            if (availableSpace <= 0)
            {
                return $"{label} {value}";
            }
            return $"{label}{value.PadLeft(availableSpace)}";
        }
    }
}

