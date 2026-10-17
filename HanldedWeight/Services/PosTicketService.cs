using System;
using System.Text;
using HandledWeight.Entities;

namespace HandledWeight.Services
{
    public class PosTicketService
    {
        // Ancho exacto para impresora de boletas/impacto Epson TM-U295 (35 columnas estándar)
        private const int MaxWidth = 35;

        public static string GenerateReceiptText(Receipts receipt, bool useEscPos = false)
        {
            return GenerateReceiptText(receipt, null, useEscPos);
        }

        public static string GenerateReceiptText(Receipts receipt, Factory? factory, bool useEscPos = false)
        {
            var sb = new StringBuilder();

            // ================= ENCABEZADO =================
            if (factory != null && !string.IsNullOrWhiteSpace(factory.Name))
            {
                sb.AppendLine(CenterLine(factory.Name));
                if (!string.IsNullOrWhiteSpace(factory.RFC)) sb.AppendLine(CenterLine($"RFC: {factory.RFC}"));
                if (!string.IsNullOrWhiteSpace(factory.Address)) sb.AppendLine(CenterLine(factory.Address));
                if (!string.IsNullOrWhiteSpace(factory.Phone)) sb.AppendLine(CenterLine($"TEL: {factory.Phone}"));
            }
            else
            {
                sb.AppendLine(CenterLine("FORRAJERA SAN RAFAEL S.A. DE C.V."));
                sb.AppendLine(CenterLine("KM.4 CARR. SAN JUAN - GDL"));
                sb.AppendLine(CenterLine("SAN JUAN DE LOS LAGOS, JAL."));
                sb.AppendLine(CenterLine("TEL: (395) 785 33 06"));
            }
            sb.AppendLine();

            // Tipo de ticket
            if (receipt.IsPublic)
            {
                sb.AppendLine(CenterLine("*** PESAJE PUBLICO ***"));
            }
            else if (receipt.WeightOut.HasValue && receipt.WeightOut > 0)
            {
                sb.AppendLine(CenterLine("*** TICKET DE SALIDA ***"));
            }
            else if (receipt.WeightOut.HasValue && receipt.WeightOut == 0)
            {
                sb.AppendLine(CenterLine("*** TICKET DE PESAJE ***"));
            }
            else
            {
                sb.AppendLine(CenterLine("*** TICKET DE ENTRADA ***"));
            }

            sb.AppendLine(new string('=', MaxWidth));

            // ================= DATOS GENERALES =================
            string partnerLabel = receipt.IsPublic ? "Cliente:" : "Proveedor:";
            sb.AppendLine(FormatLine("FOLIO:", receipt.ReceiptNumber ?? receipt.Id.ToString()));
            sb.AppendLine(FormatLine("Fecha/Hora:", receipt.CreatedAt.ToString("dd/MM/yyyy HH:mm")));
            sb.AppendLine(FormatLine(partnerLabel, receipt.BussinesPartner?.Name ?? "N/A"));
            sb.AppendLine(FormatLine("Placa:", receipt.Vehicle?.Plate ?? "N/A"));
            sb.AppendLine(FormatLine("Vehiculo:", receipt.Vehicle?.Type ?? receipt.Vehicle?.KindOfVehicle ?? "N/A"));
            sb.AppendLine(FormatLine("Chofer:", receipt.Driver?.Name ?? "N/A"));
            sb.AppendLine(FormatLine("Producto:", receipt.Product?.Name ?? "N/A"));

            if (!string.IsNullOrWhiteSpace(receipt.Origin) || !string.IsNullOrWhiteSpace(receipt.OCity))
            {
                string origStr = $"{receipt.Origin} {receipt.OCity}".Trim();
                sb.AppendLine(FormatLine("Origen:", origStr));
            }

            if (!string.IsNullOrWhiteSpace(receipt.Destination) || !string.IsNullOrWhiteSpace(receipt.DCity))
            {
                string destStr = $"{receipt.Destination} {receipt.DCity}".Trim();
                sb.AppendLine(FormatLine("Destino:", destStr));
            }

            sb.AppendLine(new string('-', MaxWidth));

            // ================= PESAJES =================
            decimal pEntrada = receipt.WeightIn ?? receipt.GrossWeight ?? 0;
            decimal pSalida = receipt.WeightOut ?? receipt.Tara ?? 0;

            if (receipt.IsPublic || (receipt.WeightOut.HasValue && receipt.WeightOut == 0))
            {
                // Pesaje único finalizado
                sb.AppendLine(FormatLine("PESO REGISTRADO:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("PESO BRUTO:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("PESO NETO:", $"{pEntrada:N0} kg"));
            }
            else if (!receipt.WeightOut.HasValue)
            {
                // Solo primera pesada (Entrada pendiente)
                sb.AppendLine(FormatLine("PESO ENTRADA:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("PESO SALIDA:", "PENDIENTE"));
                sb.AppendLine(FormatLine("PESO BRUTO:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("ESTADO:", "EN PATIO/PENDIENTE"));
            }
            else
            {
                // Lógica de dos pesadas: entrada y salida
                decimal pNeto = (pEntrada >= pSalida) ? (pEntrada - pSalida) : (pSalida - pEntrada);
                decimal pBruto = (pEntrada >= pSalida) ? pEntrada : pSalida;
                decimal pTara = (pEntrada >= pSalida) ? pSalida : pEntrada;

                sb.AppendLine(FormatLine("PESO ENTRADA:", $"{pEntrada:N0} kg"));
                sb.AppendLine(FormatLine("PESO SALIDA:", $"{pSalida:N0} kg"));
                sb.AppendLine(FormatLine("PESO BRUTO:", $"{pBruto:N0} kg"));
                sb.AppendLine(FormatLine("TARA:", $"{pTara:N0} kg"));
                sb.AppendLine(FormatLine("PESO NETO:", $"{pNeto:N0} kg"));
            }

            // Factores de ajuste (si aplican y no es público)
            if (!receipt.IsPublic && receipt.FitFactor.HasValue && receipt.FitFactor > 0)
            {
                string factorStr = receipt.FitFactor.Value >= 1 
                    ? $"{receipt.FitFactor.Value:N2}%" 
                    : $"{receipt.FitFactor.Value:P2}";
                sb.AppendLine(FormatLine("FACTOR AJUSTE:", factorStr));
                sb.AppendLine(FormatLine("PESO AJUSTADO:", $"{receipt.TotalWeightAdjust ?? 0:N0} kg"));
            }

            // Datos de precio / importe / tarifa
            if (receipt.TotalAmount > 0)
            {
                sb.AppendLine(new string('-', MaxWidth));
                if (receipt.IsPublic)
                {
                    sb.AppendLine(FormatLine("TARIFA:", $"{receipt.TotalAmount:C2}"));
                }
                else
                {
                    sb.AppendLine(FormatLine("PRECIO/KG:", $"{receipt.Price ?? 0:C2}"));
                    sb.AppendLine(FormatLine("IMPORTE TOTAL:", $"{receipt.TotalAmount:C2}"));
                }
            }

            // Observaciones
            if (!string.IsNullOrWhiteSpace(receipt.Observations))
            {
                sb.AppendLine(new string('-', MaxWidth));
                string obs = receipt.Observations.Trim();
                while (obs.Length > MaxWidth)
                {
                    sb.AppendLine(obs.Substring(0, MaxWidth));
                    obs = obs.Substring(MaxWidth);
                }
                if (obs.Length > 0) sb.AppendLine(obs);
            }

            // ================= PIE DE PÁGINA Y FIRMAS =================
            sb.AppendLine();
            sb.AppendLine(CenterLine(new string('_', 26)));
            sb.AppendLine(CenterLine("FIRMA DE CONFORMIDAD"));
            sb.AppendLine();
            sb.AppendLine(CenterLine("calidad y nutricion"));
            sb.AppendLine(CenterLine("WWW.GRUPOSANRAFAEL.COM.MX"));
            
            // Avance de boleta para que salga de la platina de la TM-U295
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();

            return SanitizeToAscii(sb.ToString());
        }

        // Centra el texto calculando espacios a la izquierda (nunca excede MaxWidth)
        public static string CenterLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = text.Trim();
            if (text.Length >= MaxWidth)
            {
                return text.Length > MaxWidth ? text.Substring(0, MaxWidth) : text;
            }
            int totalSpaces = MaxWidth - text.Length;
            int padLeft = (totalSpaces / 2) + text.Length;
            return text.PadLeft(padLeft);
        }

        // Formatea el texto con justificación a ambos lados (Etiqueta izquierda, Valor derecha) sin exceder MaxWidth
        public static string FormatLine(string label, string value)
        {
            label = label?.Trim() ?? string.Empty;
            value = value?.Trim() ?? string.Empty;
            int availableSpace = MaxWidth - label.Length;
            if (availableSpace <= 0)
            {
                return label.Length > MaxWidth ? label.Substring(0, MaxWidth) : label;
            }
            if (value.Length > availableSpace)
            {
                value = value.Substring(0, availableSpace);
            }
            return $"{label}{value.PadLeft(availableSpace)}";
        }

        // Remueve caracteres especiales, acentos y códigos de control binarios
        public static string SanitizeToAscii(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                switch (c)
                {
                    case 'á': case 'à': case 'ä': case 'â': sb.Append('a'); break;
                    case 'é': case 'è': case 'ë': case 'ê': sb.Append('e'); break;
                    case 'í': case 'ì': case 'ï': case 'î': sb.Append('i'); break;
                    case 'ó': case 'ò': case 'ö': case 'ô': sb.Append('o'); break;
                    case 'ú': case 'ù': case 'ü': case 'û': sb.Append('u'); break;
                    case 'ñ': sb.Append('n'); break;
                    case 'Á': case 'À': case 'Ä': case 'Â': sb.Append('A'); break;
                    case 'É': case 'È': case 'Ë': case 'Ê': sb.Append('E'); break;
                    case 'Í': case 'Ì': case 'Ï': case 'Î': sb.Append('I'); break;
                    case 'Ó': case 'Ò': case 'Ö': case 'Ô': sb.Append('O'); break;
                    case 'Ú': case 'Ù': case 'Ü': case 'Û': sb.Append('U'); break;
                    case 'Ñ': sb.Append('N'); break;
                    case 'º': case 'ª': case '°': sb.Append(' '); break;
                    default:
                        // Permitir solo caracteres ASCII estándar legibles (32 a 126), tabulación (9) y saltos de línea (10 y 13)
                        if ((c >= 32 && c <= 126) || c == '\n' || c == '\r' || c == '\t')
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            sb.Append(' ');
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
