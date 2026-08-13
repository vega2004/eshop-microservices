using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Tickets.API.Services;

public class TicketPdfService : ITicketPdfService
{
    private const double PageWidth = 612;
    private const double PageHeight = 792;
    private const double Margin = 42;
    private const double BottomMargin = 48;
    private const double TableHeaderHeight = 26;
    private static readonly CultureInfo MexicoCulture = CultureInfo.GetCultureInfo("es-MX");

    public byte[] Generate(OrderDto order)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        var y = DrawHeader(page, order);

        y = DrawPurchaseInfo(page, order, y - 20);
        y = DrawCustomerCard(page, order, y - 18);
        y = DrawProductsTableHeader(page, y - 20);

        var rowIndex = 0;
        foreach (var item in order.Items)
        {
            var rowHeight = GetProductRowHeight(item);

            if (y - rowHeight < BottomMargin + 145)
            {
                page = document.AddPage();
                y = DrawContinuationHeader(page, order);
                y = DrawProductsTableHeader(page, y - 16);
            }

            DrawProductRow(page, item, y, rowHeight, rowIndex);
            y -= rowHeight;
            rowIndex++;
        }

        if (y < BottomMargin + 145)
        {
            page = document.AddPage();
            y = DrawContinuationHeader(page, order) - 20;
        }

        y = DrawTotals(page, order, y - 24);
        DrawThankYou(page, y - 28);

        return document.Build(order.OrderNumber);
    }

    private static double DrawHeader(PdfPage page, OrderDto order)
    {
        const double headerTop = PageHeight - Margin;
        const double headerHeight = 92;
        var headerY = headerTop - headerHeight;
        var status = GetStatus(order.Status);

        page.FillRect(Margin, headerY, PageWidth - Margin * 2, headerHeight, Colors.Navy);
        page.FillRect(Margin + 18, headerY + 28, 38, 38, Colors.White);
        page.Text("e", Margin + 31, headerY + 38, 22, "F2", Colors.Navy);
        page.Text("E-SHOP", Margin + 68, headerY + 51, 22, "F2", Colors.White);
        page.Text("TICKET DE COMPRA", Margin + 70, headerY + 33, 12, "F1", Colors.LightBlue);

        page.Text("FOLIO", PageWidth - Margin - 150, headerY + 57, 9, "F2", Colors.LightBlue);
        page.TextRight(order.OrderNumber, PageWidth - Margin - 18, headerY + 38, 13, "F2", Colors.White);
        DrawStatusBadge(page, status, PageWidth - Margin - 132, headerY + 14, 114, 19);

        return headerY;
    }

    private static double DrawContinuationHeader(PdfPage page, OrderDto order)
    {
        var y = PageHeight - Margin - 34;
        page.FillRect(Margin, y, PageWidth - Margin * 2, 34, Colors.Navy);
        page.Text("E-SHOP", Margin + 16, y + 12, 14, "F2", Colors.White);
        page.TextRight(order.OrderNumber, PageWidth - Margin - 16, y + 12, 11, "F2", Colors.White);
        return y;
    }

    private static double DrawPurchaseInfo(PdfPage page, OrderDto order, double y)
    {
        DrawSectionTitle(page, "INFORMACION DE LA COMPRA", y);
        y -= 16;

        var cardHeight = 74;
        page.FillRect(Margin, y - cardHeight, PageWidth - Margin * 2, cardHeight, Colors.SoftGray);
        page.StrokeRect(Margin, y - cardHeight, PageWidth - Margin * 2, cardHeight, Colors.Border);

        var leftX = Margin + 18;
        var rightX = Margin + 292;
        DrawLabelValue(page, "Folio", order.OrderNumber, leftX, y - 25);
        DrawLabelValue(page, "Fecha", FormatDate(order.CreatedAt), leftX, y - 53);
        DrawLabelValue(page, "Estado", GetStatus(order.Status).Label, rightX, y - 25);
        DrawLabelValue(page, "Unidades", GetTotalUnits(order).ToString(CultureInfo.InvariantCulture), rightX, y - 53);

        return y - cardHeight;
    }

    private static double DrawCustomerCard(PdfPage page, OrderDto order, double y)
    {
        DrawSectionTitle(page, "CLIENTE", y);
        y -= 16;

        var cardHeight = 62;
        page.FillRect(Margin, y - cardHeight, PageWidth - Margin * 2, cardHeight, Colors.SoftGray);
        page.StrokeRect(Margin, y - cardHeight, PageWidth - Margin * 2, cardHeight, Colors.Border);
        DrawLabelValue(page, "Nombre", GetDisplayValue(order.CustomerUserName), Margin + 18, y - 25);
        DrawLabelValue(page, "Correo", GetDisplayValue(order.CustomerEmail), Margin + 18, y - 51);

        return y - cardHeight;
    }

    private static double DrawProductsTableHeader(PdfPage page, double y)
    {
        page.FillRect(Margin, y - TableHeaderHeight, PageWidth - Margin * 2, TableHeaderHeight, Colors.Navy);
        page.Text("PRODUCTO", Margin + 10, y - 17, 9, "F2", Colors.White);
        page.TextCenter("CANT.", ColumnQuantityX + ColumnQuantityWidth / 2, y - 17, 9, "F2", Colors.White);
        page.TextRight("PRECIO UNIT.", ColumnUnitPriceX + ColumnUnitPriceWidth - 8, y - 17, 9, "F2", Colors.White);
        page.TextRight("IMPORTE", ColumnAmountX + ColumnAmountWidth - 10, y - 17, 9, "F2", Colors.White);
        return y - TableHeaderHeight;
    }

    private static void DrawProductRow(PdfPage page, OrderItemDto item, double y, double rowHeight, int rowIndex)
    {
        var background = rowIndex % 2 == 0 ? Colors.White : Colors.SoftGray;
        page.FillRect(Margin, y - rowHeight, PageWidth - Margin * 2, rowHeight, background);
        page.Line(Margin, y - rowHeight, PageWidth - Margin, y - rowHeight, Colors.Border, 0.7);

        var nameLines = WrapText(item.ProductName, ColumnProductWidth - 18, 9);
        var textY = y - 16;
        foreach (var line in nameLines)
        {
            page.Text(line, Margin + 10, textY, 9, "F1", Colors.Text);
            textY -= 11;
        }

        page.TextCenter(item.Quantity.ToString(CultureInfo.InvariantCulture), ColumnQuantityX + ColumnQuantityWidth / 2, y - 18, 9, "F1", Colors.Text);
        page.TextRight(FormatCurrency(item.UnitPrice), ColumnUnitPriceX + ColumnUnitPriceWidth - 8, y - 18, 9, "F1", Colors.Text);
        page.TextRight(FormatCurrency(item.LineTotal), ColumnAmountX + ColumnAmountWidth - 10, y - 18, 9, "F2", Colors.Text);
    }

    private static double DrawTotals(PdfPage page, OrderDto order, double y)
    {
        const double width = 232;
        const double x = PageWidth - Margin - width;
        const double height = 108;
        var top = y;

        page.FillRect(x, top - height, width, height, Colors.White);
        page.StrokeRect(x, top - height, width, height, Colors.Border);
        page.Text("RESUMEN", x + 18, top - 22, 10, "F2", Colors.Navy);
        DrawTotalLine(page, "Subtotal", FormatCurrency(order.Subtotal), x + 18, top - 45, false);
        DrawTotalLine(page, "Impuestos", FormatCurrency(order.Tax), x + 18, top - 65, false);
        page.Line(x + 18, top - 78, x + width - 18, top - 78, Colors.Border, 0.8);
        page.FillRect(x + 12, top - 102, width - 24, 22, Colors.AccentSoft);
        page.Text("TOTAL", x + 20, top - 96, 13, "F2", Colors.Navy);
        page.TextRight(FormatCurrency(order.Total), x + width - 22, top - 96, 14, "F2", Colors.Navy);

        return top - height;
    }

    private static void DrawThankYou(PdfPage page, double y)
    {
        page.TextCenter("Gracias por su compra.", PageWidth / 2, y, 13, "F2", Colors.Navy);
        page.TextCenter("Este documento corresponde al ticket de la orden indicada.", PageWidth / 2, y - 18, 9, "F1", Colors.Muted);
        page.TextCenter("E-Shop - Compra segura", PageWidth / 2, y - 34, 9, "F2", Colors.Accent);
    }

    private static void DrawFooter(PdfPage page, string orderNumber, int pageNumber)
    {
        page.Line(Margin, 32, PageWidth - Margin, 32, Colors.Border, 0.7);
        page.Text($"Folio: {orderNumber}", Margin, 18, 8, "F1", Colors.Muted);
        page.TextRight($"Pagina {pageNumber}", PageWidth - Margin, 18, 8, "F1", Colors.Muted);
    }

    private static void DrawSectionTitle(PdfPage page, string title, double y)
    {
        page.Text(title, Margin, y, 10, "F2", Colors.Navy);
        page.Line(Margin, y - 5, PageWidth - Margin, y - 5, Colors.Accent, 1.2);
    }

    private static void DrawLabelValue(PdfPage page, string label, string value, double x, double y)
    {
        page.Text(label.ToUpperInvariant(), x, y + 10, 7, "F2", Colors.Muted);
        page.Text(RemoveNonAscii(value), x, y - 3, 10, "F1", Colors.Text);
    }

    private static void DrawTotalLine(PdfPage page, string label, string value, double x, double y, bool bold)
    {
        page.Text(label, x, y, 9, bold ? "F2" : "F1", Colors.Text);
        page.TextRight(value, PageWidth - Margin - 18, y, 9, bold ? "F2" : "F1", Colors.Text);
    }

    private static void DrawStatusBadge(PdfPage page, StatusView status, double x, double y, double width, double height)
    {
        page.FillRect(x, y, width, height, status.Background);
        page.StrokeRect(x, y, width, height, status.Border);
        page.TextCenter(status.Label.ToUpperInvariant(), x + width / 2, y + 6, 8, "F2", status.Text);
    }

    private static double GetProductRowHeight(OrderItemDto item)
    {
        var lines = WrapText(item.ProductName, ColumnProductWidth - 18, 9).Count;
        return Math.Max(30, lines * 11 + 14);
    }

    private static List<string> WrapText(string text, double maxWidth, double fontSize)
    {
        var words = RemoveNonAscii(string.IsNullOrWhiteSpace(text) ? "Producto sin nombre" : text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";

            if (EstimateTextWidth(candidate, fontSize) <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(current))
            {
                lines.Add(current);
            }

            current = word;

            while (EstimateTextWidth(current, fontSize) > maxWidth)
            {
                var splitLength = Math.Max(1, (int)Math.Floor(maxWidth / (fontSize * 0.56)));
                lines.Add(current[..Math.Min(splitLength, current.Length)]);
                current = current[Math.Min(splitLength, current.Length)..];
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            lines.Add(current);
        }

        return lines.Count == 0 ? ["Producto sin nombre"] : lines;
    }

    private static double EstimateTextWidth(string text, double fontSize)
    {
        return text.Length * fontSize * 0.52;
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("C", MexicoCulture);
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", MexicoCulture);
    }

    private static int GetTotalUnits(OrderDto order)
    {
        return order.Items.Sum(item => item.Quantity);
    }

    private static StatusView GetStatus(JsonElement status)
    {
        var statusValue = GetRawStatus(status);

        return statusValue switch
        {
            "Pending" => new StatusView("Pendiente", Colors.PendingBg, Colors.PendingBorder, Colors.PendingText),
            "Confirmed" => new StatusView("Confirmada", Colors.ConfirmedBg, Colors.ConfirmedBorder, Colors.ConfirmedText),
            "Cancelled" => new StatusView("Cancelada", Colors.CancelledBg, Colors.CancelledBorder, Colors.CancelledText),
            _ => new StatusView("Desconocido", Colors.SoftGray, Colors.Border, Colors.Text)
        };
    }

    private static string GetRawStatus(JsonElement status)
    {
        if (status.ValueKind == JsonValueKind.Number && status.TryGetInt32(out var statusNumber))
        {
            return statusNumber switch
            {
                0 => "Pending",
                1 => "Confirmed",
                2 => "Cancelled",
                _ => "Unknown"
            };
        }

        return status.ValueKind == JsonValueKind.String
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

    private const double ColumnProductWidth = 237.6;
    private const double ColumnQuantityX = Margin + ColumnProductWidth;
    private const double ColumnQuantityWidth = 52.8;
    private const double ColumnUnitPriceX = ColumnQuantityX + ColumnQuantityWidth;
    private const double ColumnUnitPriceWidth = 116.16;
    private const double ColumnAmountX = ColumnUnitPriceX + ColumnUnitPriceWidth;
    private const double ColumnAmountWidth = 121.44;

    private record StatusView(string Label, PdfColor Background, PdfColor Border, PdfColor Text);

    private readonly record struct PdfColor(double R, double G, double B);

    private static class Colors
    {
        public static readonly PdfColor Navy = new(0.06, 0.15, 0.24);
        public static readonly PdfColor White = new(1, 1, 1);
        public static readonly PdfColor Text = new(0.10, 0.14, 0.20);
        public static readonly PdfColor Muted = new(0.39, 0.45, 0.54);
        public static readonly PdfColor SoftGray = new(0.96, 0.97, 0.98);
        public static readonly PdfColor Border = new(0.84, 0.88, 0.92);
        public static readonly PdfColor LightBlue = new(0.84, 0.90, 0.97);
        public static readonly PdfColor Accent = new(0.78, 0.51, 0.13);
        public static readonly PdfColor AccentSoft = new(1.00, 0.93, 0.78);
        public static readonly PdfColor PendingBg = new(1.00, 0.95, 0.78);
        public static readonly PdfColor PendingBorder = new(0.90, 0.64, 0.18);
        public static readonly PdfColor PendingText = new(0.47, 0.30, 0.02);
        public static readonly PdfColor ConfirmedBg = new(0.83, 0.96, 0.88);
        public static readonly PdfColor ConfirmedBorder = new(0.22, 0.64, 0.39);
        public static readonly PdfColor ConfirmedText = new(0.06, 0.38, 0.19);
        public static readonly PdfColor CancelledBg = new(0.99, 0.86, 0.86);
        public static readonly PdfColor CancelledBorder = new(0.83, 0.22, 0.22);
        public static readonly PdfColor CancelledText = new(0.58, 0.08, 0.08);
    }

    private class PdfPage
    {
        private readonly StringBuilder _content = new();

        public void FillRect(double x, double y, double width, double height, PdfColor color)
        {
            _content.AppendLine($"q {Color(color, false)} {Number(x)} {Number(y)} {Number(width)} {Number(height)} re f Q");
        }

        public void StrokeRect(double x, double y, double width, double height, PdfColor color)
        {
            _content.AppendLine($"q {Color(color, true)} 0.8 w {Number(x)} {Number(y)} {Number(width)} {Number(height)} re S Q");
        }

        public void Line(double x1, double y1, double x2, double y2, PdfColor color, double width)
        {
            _content.AppendLine($"q {Color(color, true)} {Number(width)} w {Number(x1)} {Number(y1)} m {Number(x2)} {Number(y2)} l S Q");
        }

        public void Text(string text, double x, double y, double size, string font, PdfColor color)
        {
            _content.AppendLine($"BT {Color(color, false)} /{font} {Number(size)} Tf {Number(x)} {Number(y)} Td ({EscapePdfText(RemoveNonAscii(text))}) Tj ET");
        }

        public void TextRight(string text, double rightX, double y, double size, string font, PdfColor color)
        {
            Text(text, rightX - EstimateTextWidth(RemoveNonAscii(text), size), y, size, font, color);
        }

        public void TextCenter(string text, double centerX, double y, double size, string font, PdfColor color)
        {
            Text(text, centerX - EstimateTextWidth(RemoveNonAscii(text), size) / 2, y, size, font, color);
        }

        public string Content => _content.ToString();
    }

    private class PdfDocument
    {
        private readonly List<PdfPage> _pages = [];

        public PdfPage AddPage()
        {
            var page = new PdfPage();
            _pages.Add(page);
            return page;
        }

        public byte[] Build(string orderNumber)
        {
            for (var index = 0; index < _pages.Count; index++)
            {
                DrawFooter(_pages[index], orderNumber, index + 1);
            }

            var pageReferences = string.Join(
                " ",
                Enumerable.Range(0, _pages.Count).Select(pageIndex => $"{3 + pageIndex * 2} 0 R"));
            var normalFontObjectNumber = 3 + _pages.Count * 2;
            var boldFontObjectNumber = normalFontObjectNumber + 1;

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                $"<< /Type /Pages /Kids [{pageReferences}] /Count {_pages.Count} >>"
            };

            for (var index = 0; index < _pages.Count; index++)
            {
                var pageObjectNumber = 3 + index * 2;
                var contentObjectNumber = pageObjectNumber + 1;
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(PageWidth)} {Number(PageHeight)}] /Resources << /Font << /F1 {normalFontObjectNumber} 0 R /F2 {boldFontObjectNumber} 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
                var content = _pages[index].Content;
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream");
            }

            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

            return BuildPdf(objects);
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
    }

    private static string Color(PdfColor color, bool stroke)
    {
        return $"{Number(color.R)} {Number(color.G)} {Number(color.B)} {(stroke ? "RG" : "rg")}";
    }

    private static string Number(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
