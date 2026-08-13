using System.Globalization;
using System.Text;

namespace Tickets.API.Services;

public class TicketPdfService : ITicketPdfService
{
    private static readonly CultureInfo MexicoCulture = CultureInfo.GetCultureInfo("es-MX");

    public byte[] Generate(OrderDto order)
    {
        var lines = BuildLines(order);
        var content = BuildContentStream(lines);
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"
        };

        return BuildPdf(objects);
    }

    private static List<string> BuildLines(OrderDto order)
    {
        var lines = new List<string>
        {
            "E-SHOP",
            "TICKET DE COMPRA",
            string.Empty,
            $"Folio: {order.OrderNumber}",
            $"Fecha: {order.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}",
            $"Estado: {GetStatusLabel(order.Status)}",
            $"Cliente: {GetDisplayValue(order.CustomerUserName)}",
            $"Correo: {GetDisplayValue(order.CustomerEmail)}",
            string.Empty,
            "Producto | Cantidad | Precio unitario | Importe"
        };

        foreach (var item in order.Items)
        {
            lines.Add($"{item.ProductName} | {item.Quantity} | {FormatCurrency(item.UnitPrice)} | {FormatCurrency(item.LineTotal)}");
        }

        lines.Add(string.Empty);
        lines.Add($"Subtotal: {FormatCurrency(order.Subtotal)}");
        lines.Add($"Impuestos: {FormatCurrency(order.Tax)}");
        lines.Add($"Total: {FormatCurrency(order.Total)}");
        lines.Add(string.Empty);
        lines.Add("Gracias por su compra.");
        lines.Add("Este documento corresponde al ticket de la orden indicada.");

        return lines;
    }

    private static string BuildContentStream(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");
        builder.AppendLine("/F1 12 Tf");
        builder.AppendLine("50 742 Td");

        foreach (var line in lines)
        {
            builder.Append("(");
            builder.Append(EscapePdfText(RemoveNonAscii(line)));
            builder.AppendLine(") Tj");
            builder.AppendLine("0 -18 Td");
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }

    private static byte[] BuildPdf(IReadOnlyList<string> objects)
    {
        var builder = new StringBuilder();
        var offsets = new List<int> { 0 };

        builder.AppendLine("%PDF-1.4");

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.AppendLine($"{index + 1} 0 obj");
            builder.AppendLine(objects[index]);
            builder.AppendLine("endobj");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.AppendLine("xref");
        builder.AppendLine($"0 {objects.Count + 1}");
        builder.AppendLine("0000000000 65535 f ");

        for (var index = 1; index < offsets.Count; index++)
        {
            builder.AppendLine($"{offsets[index]:D10} 00000 n ");
        }

        builder.AppendLine("trailer");
        builder.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        builder.AppendLine("startxref");
        builder.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("C", MexicoCulture);
    }

    private static string GetStatusLabel(System.Text.Json.JsonElement status)
    {
        if (status.ValueKind == System.Text.Json.JsonValueKind.Number && status.TryGetInt32(out var statusNumber))
        {
            return statusNumber switch
            {
                0 => "Pending",
                1 => "Confirmed",
                2 => "Cancelled",
                _ => "Unknown"
            };
        }

        return status.ValueKind == System.Text.Json.JsonValueKind.String
            ? status.GetString() ?? "Unknown"
            : "Unknown";
    }

    private static string GetDisplayValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "No disponible" : value;
    }

    private static string EscapePdfText(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static string RemoveNonAscii(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character <= 127 ? character : '?');
        }

        return builder.ToString();
    }
}
