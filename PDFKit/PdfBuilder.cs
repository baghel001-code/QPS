using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.UniversalAccessibility.Drawing;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PDFKit;

/// <summary>
/// IMPORTANT: register a font resolver once at startup before creating any
/// PdfBuilder - see SimpleFontResolver.cs for why and how.
/// </summary>
public sealed class PdfBuilder : IDisposable
{
    private readonly PdfDocument _document;
    private readonly PageSettings _settings;
    private PdfPage _currentPage;
    private XGraphics _gfx;
    private double _cursorY;
    private readonly double _contentWidth;
    private readonly double _pageHeight;
    private bool _footerPending;
    private bool _footersApplied;
    private string? _footerTitle;

    public PdfBuilder(PageSettings? settings = null)
    {
        _settings = settings ?? new PageSettings();
        _document = new PdfDocument();
        _currentPage = _document.AddPage();
        ApplyPageSize(_currentPage);
        _gfx = XGraphics.FromPdfPage(_currentPage);

        _contentWidth = _currentPage.Width.Point - _settings.MarginLeft - _settings.MarginRight;
        _pageHeight = _currentPage.Height.Point;
        _cursorY = _settings.MarginTop;

    }

    private void ApplyPageSize(PdfPage page)
    {
        page.Size = PdfSharp.PageSize.A4;
        if (_settings.Landscape)
        {
            page.Orientation = PdfSharp.PageOrientation.Landscape;
        }
    }

    /// <summary>
    /// Ensures there's at least `neededHeight` of vertical space left on the
    /// current page. If not, starts a fresh page and resets the cursor.
    /// </summary>
    private void EnsureSpace(double neededHeight)
    {
        double bottomLimit = _pageHeight - _settings.MarginBottom;
        if (_cursorY + neededHeight > bottomLimit)
        {
            _gfx.Dispose();
            _currentPage = _document.AddPage();
            ApplyPageSize(_currentPage);
            _gfx = XGraphics.FromPdfPage(_currentPage);
            _cursorY = _settings.MarginTop;
        }
    }

    private static XFont GetFont(TextStyle style) => style switch
    {
        TextStyle.Title => new XFont("Arial", 20, XFontStyleEx.Bold),
        TextStyle.Heading => new XFont("Arial", 14, XFontStyleEx.Bold),
        TextStyle.SubHeading => new XFont("Arial", 12, XFontStyleEx.Bold),
        TextStyle.Body => new XFont("Arial", 10, XFontStyleEx.Regular),
        TextStyle.Small => new XFont("Arial", 8, XFontStyleEx.Regular),
        TextStyle.SmallBold => new XFont("Arial", 8, XFontStyleEx.Bold),
        TextStyle.ResultChip => new XFont("Arial", 24, XFontStyleEx.Bold),
        _ => new XFont("Arial", 10, XFontStyleEx.Regular)
    };

    /// <summary>
    /// Draws a letterhead block: optional logo, company name, document title,
    /// reference number, and a horizontal rule beneath it. Call once at the top.
    /// </summary>
    public PdfBuilder AddLetterhead(LetterheadInfo info)
    {
        EnsureSpace(70);
        double x = _settings.MarginLeft;
        if (!string.IsNullOrEmpty(info.LogoPath) && File.Exists(info.LogoPath))
        {
            var img = XImage.FromFile(info.LogoPath);
            double logoHeight = 32;
            double logoWidth = img.PixelWidth * (logoHeight / img.PixelHeight);
            _gfx.DrawImage(img, x, _cursorY, logoWidth, logoHeight);
            _cursorY += logoHeight+6;
        }

        if (!string.IsNullOrEmpty(info.DateOfInspection))
        {
            _gfx.DrawString(info.DateOfInspection, new XFont("Arial", 10, XFontStyleEx.Bold), BrandBrush,
           new XRect(x, _cursorY, _contentWidth - (x - _settings.MarginLeft), 24), XStringFormats.TopRight);
        }
        if (!string.IsNullOrEmpty(info.CompanyName))
        {
            _gfx.DrawString(info.CompanyName, GetFont(TextStyle.Heading), BrandBrush,
                new XRect(_settings.MarginLeft, _cursorY, _contentWidth, 24), XStringFormats.TopLeft);
            _cursorY += 20;
        }
        if (!string.IsNullOrEmpty(info.Last_Amendment_Date))
        {
            _cursorY -= 8;
            _gfx.DrawString(info.Last_Amendment_Date, GetFont(TextStyle.SmallBold), XBrushes.Gray,
           new XRect(x, _cursorY, _contentWidth - (x - _settings.MarginLeft), 14), XStringFormats.TopRight);
            _cursorY += 10;
        }

        if (!string.IsNullOrEmpty(info.ReferenceNumber))
        {
            var refFont = GetFont(TextStyle.Body);
            _gfx.DrawString(info.ReferenceNumber, new XFont("Arial", 10, XFontStyleEx.Bold), XBrushes.Black,
                new XRect(_settings.MarginLeft, _cursorY, _contentWidth, 16), XStringFormats.TopRight);
        }

        if (!string.IsNullOrEmpty(info.Tagline))
        {
            _gfx.DrawString(info.Tagline, GetFont(TextStyle.Small), XBrushes.Gray,
                new XRect(_settings.MarginLeft, _cursorY, _contentWidth, 14), XStringFormats.TopLeft);
            _cursorY += 12;
        }

        if (!string.IsNullOrEmpty(info.DocumentTitle))
        {
            _gfx.DrawString(info.DocumentTitle, GetFont(TextStyle.Body), XBrushes.Black,
                new XRect(_settings.MarginLeft, _cursorY, _contentWidth, 28), XStringFormats.Center);
            _cursorY += 25;
            //_footerTitle = info.DocumentTitle;
        }

        if (!string.IsNullOrEmpty(info.FooterInfo))
        {
            _footerTitle = info.FooterInfo;
        }

        _gfx.DrawLine(XPens.Gray, _settings.MarginLeft, _cursorY, _settings.MarginLeft + _contentWidth, _cursorY);
        _cursorY += 8;

        return this;
    }

    public PdfBuilder AddText(string text, TextStyle style = TextStyle.Body, TextAlign align = TextAlign.Left)
    {
        var font = GetFont(style);
        double lineHeight = font.Height + 4;

        var lines = WrapText(text, font, _contentWidth);
        var format = align switch
        {
            TextAlign.Center => XStringFormats.TopCenter,
            TextAlign.Right => XStringFormats.TopRight,
            _ => XStringFormats.TopLeft
        };

        foreach (var line in lines)
        {
            EnsureSpace(lineHeight);
            _gfx.DrawString(line, font, XBrushes.Black,
                new XRect(_settings.MarginLeft, _cursorY, _contentWidth, lineHeight), format);
            _cursorY += lineHeight;
        }

        _cursorY += 4; // paragraph spacing
        return this;
    }


    public PdfBuilder AddSpacing(double points = 10)
    {
        _cursorY += points;
        return this;
    }

    /// <summary>
    /// Renders a simple label/value pair, e.g. "Approved By: Ram Prakash".
    /// </summary>

    public PdfBuilder AddKeyValue(string key, string value)
    {
        var font = GetFont(TextStyle.Body);
        var boldFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        double lineHeight = font.Height + 6;

        double keyWidth = 140;
        double valueWidth = _contentWidth - keyWidth;
        var valueLines = WrapText(value ?? "", font, valueWidth - 4);
        double rowHeight = lineHeight * valueLines.Count;

        EnsureSpace(rowHeight);

        _gfx.DrawString(key, boldFont, XBrushes.Black,
            new XRect(_settings.MarginLeft, _cursorY, keyWidth, lineHeight), XStringFormats.TopLeft);

        for (int i = 0; i < valueLines.Count; i++)
        {
            _gfx.DrawString(valueLines[i], font, XBrushes.Black,
                new XRect(_settings.MarginLeft + keyWidth, _cursorY + i * lineHeight, valueWidth, lineHeight),
                XStringFormats.TopLeft);
        }

        _gfx.DrawLine(XPens.LightGray, _settings.MarginLeft, _cursorY + rowHeight,
            _settings.MarginLeft + _contentWidth, _cursorY + rowHeight);
        _cursorY += rowHeight;
        return this;
    }
    /// <summary>
    /// Draws a table with header row + data rows. Rows are checked for
    /// space individually, so a table can split cleanly across pages.
    /// </summary>
    /// 

    public PdfBuilder AddTable(IReadOnlyList<TableColumn> columns, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        double totalWeight = columns.Sum(c => c.Weight);
        double[] colWidths = columns.Select(c => _contentWidth * (c.Weight / totalWeight)).ToArray();

        var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        var bodyFont = GetFont(TextStyle.Body);
        var boldBodyFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        double singleLineHeight = bodyFont.Height + 10;

        void DrawHeaderRow()
        {
            EnsureSpace(singleLineHeight);
            double x = _settings.MarginLeft;
            var headerRect = new XRect(_settings.MarginLeft, _cursorY, _contentWidth, singleLineHeight);
            _gfx.DrawRectangle(BrandBrush, headerRect);

            for (int i = 0; i < columns.Count; i++)
            {
                var format = ToFormat(columns[i].Align);
                _gfx.DrawString(columns[i].Header, headerFont, XBrushes.White,
                    new XRect(x + 4, _cursorY, colWidths[i] - 8, singleLineHeight), format);
                x += colWidths[i];
            }
            _cursorY += singleLineHeight;
        }

        DrawHeaderRow();

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var cellLines = new List<string>[columns.Count];
            int maxLines = 1;
            for (int i = 0; i < columns.Count && i < row.Count; i++)
            {
                cellLines[i] = WrapText(row[i] ?? "", bodyFont, colWidths[i] - 8);
                maxLines = Math.Max(maxLines, cellLines[i].Count);
            }

            double rowHeight = singleLineHeight * maxLines;

            bool willBreak = _cursorY + rowHeight > _pageHeight - _settings.MarginBottom;
            if (willBreak)
            {
                DrawHeaderRow();
            }

            double x = _settings.MarginLeft;
            for (int i = 0; i < columns.Count && i < row.Count; i++)
            {

                var format = ToFormat(columns[i].Align);
                var lines = cellLines[i] ?? new List<string> { "" };
                double verticalOffset = (rowHeight - singleLineHeight * lines.Count) / 2;
                for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
                {
                    _gfx.DrawString(lines[lineIdx], bodyFont, XBrushes.Black,
                        new XRect(x + 4, _cursorY + verticalOffset + lineIdx * singleLineHeight, colWidths[i] - 8, singleLineHeight), format);
                }

                x += colWidths[i];
            }
            _gfx.DrawLine(XPens.LightGray, _settings.MarginLeft, _cursorY + rowHeight,
                _settings.MarginLeft + _contentWidth, _cursorY + rowHeight);
            _cursorY += rowHeight;
        }

        _cursorY += 10;
        return this;
    }

    public PdfBuilder AddTable(IReadOnlyList<TableColumn> columns, IReadOnlyList<IReadOnlyList<string>> rows, ISet<int>? chipColumns = null, ISet<int>? boldRows = null)
    {
        double totalWeight = columns.Sum(c => c.Weight);
        double[] colWidths = columns.Select(c => _contentWidth * (c.Weight / totalWeight)).ToArray();

        var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        var bodyFont = GetFont(TextStyle.Body);
        var boldBodyFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        double singleLineHeight = bodyFont.Height + 10;

        void DrawHeaderRow()
        {
            EnsureSpace(singleLineHeight);
            double x = _settings.MarginLeft;
            var headerRect = new XRect(_settings.MarginLeft, _cursorY, _contentWidth, singleLineHeight);
            _gfx.DrawRectangle(BrandBrush, headerRect);

            for (int i = 0; i < columns.Count; i++)
            {
                var format = ToFormat(columns[i].Align);
                _gfx.DrawString(columns[i].Header, headerFont, XBrushes.White,
                    new XRect(x + 4, _cursorY, colWidths[i] - 8, singleLineHeight), format);
                x += colWidths[i];
            }
            _cursorY += singleLineHeight;
        }

        DrawHeaderRow();

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            bool isBold = boldRows?.Contains(rowIndex) ?? false;
            var font = isBold ? boldBodyFont : bodyFont;

            // Wrap each non-chip cell within its column width first, so the
            // row height can grow to fit the tallest cell instead of long
            // text overflowing into the next column.
            var cellLines = new List<string>[columns.Count];
            int maxLines = 1;
            for (int i = 0; i < columns.Count && i < row.Count; i++)
            {
                bool asChip = chipColumns?.Contains(i) ?? false;
                if (asChip)
                {
                    cellLines[i] = new List<string> { row[i] ?? "" }; // chips render as one line, vertically centered
                }
                else
                {
                    cellLines[i] = WrapText(row[i] ?? "", font, colWidths[i] - 8);
                    maxLines = Math.Max(maxLines, cellLines[i].Count);
                }
            }

            double rowHeight = singleLineHeight * maxLines;

            bool willBreak = _cursorY + rowHeight > _pageHeight - _settings.MarginBottom;
            if (willBreak)
            {
                DrawHeaderRow();
            }

            double x = _settings.MarginLeft;
            for (int i = 0; i < columns.Count && i < row.Count; i++)
            {
                bool asChip = chipColumns?.Contains(i) ?? false;
                if (asChip)
                {
                    DrawCellChip(x, colWidths[i], _cursorY, rowHeight, row[i] ?? "", columns[i].Align);
                }
                else
                {
                    var format = ToFormat(columns[i].Align);
                    var lines = cellLines[i] ?? new List<string> { "" };
                    // Center this cell's text block vertically within the
                    // row when it has fewer lines than the tallest cell
                    // (e.g. a 1-line number next to a 2-line wrapped label).
                    double verticalOffset = (rowHeight - singleLineHeight * lines.Count) / 2;
                    for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
                    {
                        _gfx.DrawString(lines[lineIdx], font, XBrushes.Black,
                            new XRect(x + 4, _cursorY + verticalOffset + lineIdx * singleLineHeight, colWidths[i] - 8, singleLineHeight), format);
                    }
                }
                x += colWidths[i];
            }
            _gfx.DrawLine(XPens.LightGray, _settings.MarginLeft, _cursorY + rowHeight,
                _settings.MarginLeft + _contentWidth, _cursorY + rowHeight);
            _cursorY += rowHeight;
        }

        _cursorY += 10;
        return this;
    }

    public PdfBuilder AddTable(
    IReadOnlyList<TableColumn> columns,
    IReadOnlyList<IReadOnlyList<string>> rows,
    ISet<int>? chipColumns = null,
    ISet<int>? boldRows = null,
    ISet<int>? iconRows = null)
    {
        double totalWeight = columns.Sum(c => c.Weight);
        double[] colWidths = columns.Select(c => _contentWidth * (c.Weight / totalWeight)).ToArray();

        var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        var bodyFont = GetFont(TextStyle.Body);
        var boldBodyFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        double singleLineHeight = bodyFont.Height + 10;

        void DrawHeaderRow()
        {
            EnsureSpace(singleLineHeight);
            double x = _settings.MarginLeft;
            var headerRect = new XRect(_settings.MarginLeft, _cursorY, _contentWidth, singleLineHeight);
            _gfx.DrawRectangle(CreateBrandGradientBrush(headerRect), headerRect);
            for (int i = 0; i < columns.Count; i++)
            {
                var format = ToFormat(columns[i].Align);
                _gfx.DrawString(columns[i].Header, headerFont, XBrushes.White,
                    new XRect(x + 4, _cursorY, colWidths[i] - 8, singleLineHeight), format);
                x += colWidths[i];
            }
            _cursorY += singleLineHeight;
        }

        DrawHeaderRow();

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            bool isBold = boldRows?.Contains(rowIndex) ?? false;

            var font = isBold ? boldBodyFont : bodyFont;

            // Wrap each non-chip cell within its column width first, so the
            // row height can grow to fit the tallest cell instead of long
            // text overflowing into the next column.
            var cellLines = new List<string>[columns.Count];
            int maxLines = 1;
            for (int i = 0; i < columns.Count && i < row.Count; i++)
            {
                bool asChip = chipColumns?.Contains(i) ?? false;
                if (asChip)
                {
                    cellLines[i] = new List<string> { row[i] ?? "" }; // chips render as one line, vertically centered
                }
                else
                {
                    cellLines[i] = WrapText(row[i] ?? "", font, colWidths[i] - 8);
                    maxLines = Math.Max(maxLines, cellLines[i].Count);
                }
            }

            double rowHeight = singleLineHeight * maxLines;

            bool willBreak = _cursorY + rowHeight > _pageHeight - _settings.MarginBottom;
            if (willBreak)
            {
                DrawHeaderRow();
            }

            double x = _settings.MarginLeft;
            for (int i = 0; i < columns.Count && i < row.Count; i++)
            {
                bool asChip = chipColumns?.Contains(i) ?? false;
                if (asChip)
                {
                    bool withIcon = iconRows?.Contains(rowIndex) ?? false;
                    DrawCellChip(x, colWidths[i], _cursorY, rowHeight, row[i] ?? "", columns[i].Align, withIcon);
                }
                else
                {
                    var format = ToFormat(columns[i].Align);
                    var lines = cellLines[i] ?? new List<string> { "" };

                    double verticalOffset = (rowHeight - singleLineHeight * lines.Count) / 2;
                    for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
                    {
                        _gfx.DrawString(lines[lineIdx], font, XBrushes.Black,
                            new XRect(x + 4, _cursorY + verticalOffset + lineIdx * singleLineHeight, colWidths[i] - 8, singleLineHeight), format);
                    }
                }
                x += colWidths[i];
            }
            _gfx.DrawLine(XPens.LightGray, _settings.MarginLeft, _cursorY + rowHeight,
                _settings.MarginLeft + _contentWidth, _cursorY + rowHeight);
            _cursorY += rowHeight;
        }

        _cursorY += 10;
        return this;
    }
    private static XStringFormat ToFormat(TextAlign align) => align switch
    {
        TextAlign.Center => XStringFormats.TopCenter,
        TextAlign.Right => XStringFormats.CenterRight,
        _ => XStringFormats.CenterLeft
    };

    // Vishal Mega Mart brand magenta/pink, used for table headers and accents.
    private static readonly XColor BrandColor = XColor.FromArgb(255, 216, 27, 96);
    private static readonly XColor GradientStart = XColor.FromArgb(255, 91, 62, 150);  // muted purple (left)
    private static readonly XColor GradientEnd = XColor.FromArgb(255, 33, 90, 168);  // blue (right, same as before)

    private static readonly XBrush BrandBrush = new XSolidBrush(BrandColor);
    private static readonly XBrush BrandBrushBlue = new XSolidBrush(GradientEnd);

    /// <summary>
    /// The pink -> blue gradient used for section/table header bars,
    /// matching the VMM brand swatch. Built fresh per call since
    /// XLinearGradientBrush is defined relative to a specific rect.
    /// </summary>
    private static XLinearGradientBrush CreateBrandGradientBrush(XRect rect, XColor? startColor = null, XColor? endColor = null) => new(
        rect,
        startColor ?? XColor.FromArgb(255, 236, 0, 140),   // hot pink (left)
        endColor ?? XColor.FromArgb(255, 33, 90, 168),   // blue (right)
        XLinearGradientMode.Horizontal);


    /// <summary>
    /// Draws a full gradient header bar with a single centered-left title -
    /// use for full-width section headers/banners with one title (e.g. a
    /// colspan header row). For multi-column header rows where each
    /// column needs its own text, draw the gradient via
    /// CreateBrandGradientBrush directly and render each column's text
    /// separately on top (see AddTable/DrawTableBlock).
    /// </summary>
    private void DrawSectionHeader(XGraphics gfx, string title, double x, double y, double width, double height)
    {
        var rect = new XRect(x, y, width, height);
        gfx.DrawRectangle(CreateBrandGradientBrush(rect), rect);

        var font = GetFont(TextStyle.SmallBold);
        gfx.DrawString(
            title,
            font,
            XBrushes.White,
            new XRect(x + 6, y, width - 12, height),
            XStringFormats.CenterLeft);
    }
    /// <summary>
    /// Maps common status values (PASS/FAIL/YES/NO/NA) to a chip color, so
    /// table cells can render them as colored pills instead of plain text.
    /// </summary>
    private static ChipColor StatusColorFor(string? value)
    {
        var v = (value ?? "").Trim().ToUpperInvariant();
        return v switch
        {
            "PASS" or "YES" => ChipColor.Success,
            "FAIL" or "NO" => ChipColor.Error,
            _ => ChipColor.Default
        };
    }

    /// <summary>Draws a small colored pill inside a table cell, left-aligned.</summary>
    private void DrawCellChip(double x, double colWidth, double y, double rowHeight, string text, TextAlign align)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        const double padding = 6;
        var font = new XFont("Arial", 9, XFontStyleEx.Bold);
        double chipHeight = Math.Min(rowHeight - 4, font.Height + 8);
        double chipWidth = _gfx.MeasureString(text, font).Width + 16;
        double chipY = y + (rowHeight - chipHeight) / 2;

        double chipX = align switch
        {
            TextAlign.Right => x + colWidth - chipWidth - padding,
            TextAlign.Center => x + (colWidth - chipWidth) / 2,
            _ => x + padding
        };

        var (fill, textBrush) = GetChipBrushes(StatusColorFor(text));
        var rect = new XRect(chipX, chipY, chipWidth, chipHeight);
        _gfx.DrawRoundedRectangle(fill, rect, new XSize(chipHeight / 2, chipHeight / 2));
        _gfx.DrawString(text, font, textBrush, rect, XStringFormats.Center);
    }

    private void DrawCellChip(double x, double colWidth, double y, double rowHeight, string text, TextAlign align, bool showStatusIcon = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        const double padding = 6;
        var font = new XFont("Arial", 9, XFontStyleEx.Bold);
        double chipHeight = Math.Min(rowHeight - 4, font.Height + 8);
        var chipColor = StatusColorFor(text);

        double iconSize = showStatusIcon ? chipHeight * 0.65 : 0;
        double iconGap = showStatusIcon ? 4 : 0;
        double textWidth = _gfx.MeasureString(text, font).Width;
        double chipWidth = textWidth + 16 + iconSize + iconGap;
        double chipY = y + (rowHeight - chipHeight) / 2;

        double chipX = align switch
        {
            TextAlign.Right => x + colWidth - chipWidth - padding,
            TextAlign.Center => x + (colWidth - chipWidth) / 2,
            _ => x + padding
        };

        var (fill, textBrush) = showStatusIcon ? GetSolidChipBrushes(chipColor) : GetChipBrushes(chipColor);
        var rect = new XRect(chipX, chipY, chipWidth, chipHeight);
        _gfx.DrawRoundedRectangle(fill, rect, new XSize(chipHeight / 2, chipHeight / 2));

        double textX = chipX + 8;
        if (showStatusIcon)
        {
            double iconCx = chipX + 8 + iconSize / 2;
            double iconCy = chipY + chipHeight / 2;
            DrawStatusIcon(_gfx, iconCx, iconCy, iconSize, chipColor == ChipColor.Success);
            textX = chipX + 8 + iconSize + iconGap;
        }

        var textRect = new XRect(textX, chipY, chipX + chipWidth - textX - 8, chipHeight);
        _gfx.DrawString(text, font, textBrush, textRect, showStatusIcon ? XStringFormats.CenterLeft : XStringFormats.Center);
    }

    private static void DrawStatusIcon(XGraphics gfx, double cx, double cy, double size, bool isSuccess)
    {
        var symbolColor = isSuccess ? XColor.FromArgb(255, 30, 178, 80) : XColor.FromArgb(255, 211, 47, 47);
        double r = size / 2;

        // White circle background, with the checkmark/exclamation drawn IN
        // the status color on top - matches MudBlazor's filled chip icon
        // look (a light icon glyph inside the chip reads as "white circle,
        // colored symbol" against a solid colored chip background).
        gfx.DrawEllipse(XBrushes.White, cx - r, cy - r, size, size);

        var symbolPen = new XPen(symbolColor, Math.Max(1.0, size * 0.16))
        {
            LineCap = XLineCap.Round,
            LineJoin = XLineJoin.Round
        };

        if (isSuccess)
        {
            // Checkmark: short stroke down-right, then long stroke up-right.
            var p1 = new XPoint(cx - r * 0.5, cy + r * 0.05);
            var p2 = new XPoint(cx - r * 0.12, cy + r * 0.4);
            var p3 = new XPoint(cx + r * 0.5, cy - r * 0.35);
            gfx.DrawLine(symbolPen, p1, p2);
            gfx.DrawLine(symbolPen, p2, p3);
        }
        else
        {
            // Exclamation mark: vertical stroke + a dot below it.
            gfx.DrawLine(symbolPen, cx, cy - r * 0.5, cx, cy + r * 0.12);
            double dotSize = size * 0.16;
            gfx.DrawEllipse(new XSolidBrush(symbolColor), cx - dotSize / 2, cy + r * 0.35 - dotSize / 2, dotSize, dotSize);
        }
    }

    private static (XBrush Fill, XBrush Text) GetChipBrushes(ChipColor color) => color switch
    {
        ChipColor.Success => (new XSolidBrush(XColor.FromArgb(255, 224, 242, 224)), new XSolidBrush(XColor.FromArgb(255, 30, 110, 30))),
        ChipColor.Error => (new XSolidBrush(XColor.FromArgb(255, 250, 220, 220)), new XSolidBrush(XColor.FromArgb(255, 150, 30, 30))),
        ChipColor.Warning => (new XSolidBrush(XColor.FromArgb(255, 255, 236, 204)), new XSolidBrush(XColor.FromArgb(255, 150, 100, 0))),
        ChipColor.Secondary => (new XSolidBrush(XColor.FromArgb(255, 252, 228, 236)), new XSolidBrush(BrandColor)),
        _ => (new XSolidBrush(XColor.FromArgb(255, 235, 235, 235)), XBrushes.Black as XBrush)
    };


    private static (XBrush Fill, XBrush Text) GetSolidChipBrushes(ChipColor color) => color switch
    {
        ChipColor.Success => (new XSolidBrush(XColor.FromArgb(255, 30, 178, 80)), XBrushes.White as XBrush),
        ChipColor.Error => (new XSolidBrush(XColor.FromArgb(255, 211, 47, 47)), XBrushes.White as XBrush),
        ChipColor.Warning => (new XSolidBrush(XColor.FromArgb(255, 237, 155, 0)), XBrushes.White as XBrush),
        ChipColor.Secondary => (new XSolidBrush(BrandColor), XBrushes.White as XBrush),
        _ => (new XSolidBrush(XColor.FromArgb(255, 120, 120, 120)), XBrushes.White as XBrush)
    };
    /// <summary>
    /// Draws a small pill-shaped label, mirroring MudBlazor's MudChip -
    /// used for the PASS/FAIL badge and section headers like
    /// "Defects Observed" / "QA Documents".
    /// </summary>
    public PdfBuilder AddChip(string text, ChipColor color = ChipColor.Default, TextAlign align = TextAlign.Left, TextStyle style = TextStyle.Body)
    {
        var font = GetFont(style);
        double chipHeight = font.Height + 6;
        double chipWidth = _gfx.MeasureString(text, font).Width + 24;
        EnsureSpace(chipHeight + 6);

        var (fill, textBrush) = GetChipBrushes(color);
        double x = align switch
        {
            TextAlign.Center => _settings.MarginLeft + (_contentWidth - chipWidth) / 2,
            TextAlign.Right => _settings.MarginLeft + _contentWidth - chipWidth,
            _ => _settings.MarginLeft
        };

        var rect = new XRect(x, _cursorY, chipWidth, chipHeight);
        _gfx.DrawRoundedRectangle(fill, rect, new XSize(chipHeight / 2, chipHeight / 2));
        _gfx.DrawString(text, font, textBrush, rect, XStringFormats.Center);
        _cursorY += chipHeight;
        return this;
    }


    public PdfBuilder AddHyperlink(string text, string url, TextAlign align = TextAlign.Left)
    {
        var font = GetFont(TextStyle.Body);
        double lineHeight = font.Height + 6;
        EnsureSpace(lineHeight);

        var linkColor = XColor.FromArgb(255, 30, 90, 200); // link blue
        var linkBrush = new XSolidBrush(linkColor);
        double textWidth = _gfx.MeasureString(text, font).Width;

        double x = align switch
        {
            TextAlign.Center => _settings.MarginLeft + (_contentWidth - textWidth) / 2,
            TextAlign.Right => _settings.MarginLeft + _contentWidth - textWidth,
            _ => _settings.MarginLeft
        };

        var textRect = new XRect(x, _cursorY, textWidth, lineHeight);
        _gfx.DrawString(text, font, linkBrush, textRect, XStringFormats.CenterLeft);

        double underlineY = _cursorY + lineHeight * 0.72;
        //_gfx.DrawLine(new XPen(linkColor, 0.6), x, underlineY, x + textWidth, underlineY); // needs XColor


        // Convert to PDF page space (bottom-left origin, y-up) for the
        // clickable annotation - this is the one place in the whole class
        // that needs this flip, since everything else stays in our
        // top-down cursor coordinates.
        double pdfY = _currentPage.Height.Point - (_cursorY + lineHeight);
        var linkRect = new XRect(x, pdfY, textWidth, lineHeight);
        _currentPage.AddWebLink(new PdfRectangle(linkRect), url);

        _cursorY += lineHeight;
        return this;
    }

    public PdfBuilder AddSectionBar(string title, XColor? startColor = null, XColor? endColor = null)
    {
        var font = GetFont(TextStyle.SmallBold);
        double barHeight = font.Height + 4;
        EnsureSpace(barHeight + 4);

        //var start = startColor ?? XColor.FromArgb(255, 31, 78, 160);  // medium blue
        //var end = endColor ?? XColor.FromArgb(255, 20, 55, 120);      // darker blue

        var rect = new XRect(_settings.MarginLeft, _cursorY, _contentWidth, barHeight);
        //var gradientBrush = new XLinearGradientBrush(rect, start, end, XLinearGradientMode.Horizontal);
        //_gfx.DrawRectangle(gradientBrush, rect);

        _gfx.DrawRectangle(CreateBrandGradientBrush(rect, startColor, endColor), rect);
        _gfx.DrawString(title, font, XBrushes.White,
            new XRect(_settings.MarginLeft + 6, _cursorY, _contentWidth - 12, barHeight),
            XStringFormats.CenterLeft);

        _cursorY += barHeight + 4;
        return this;
    }

    /// <summary>
    /// Draws two side-by-side signature/stamp blocks (e.g. "Name of VMM
    /// QA" / "Name of the Factory Representative"): a gradient header bar,
    /// the person's name in bold, then a signature image slot and a stamp
    /// image slot - each falling back to a plain gray label ("Signature" /
    /// "Stamp") if no image path is provided or the file can't be loaded.
    ///
    /// Image paths in `left`/`right` must already be LOCAL file paths -
    /// see the note on SignatureBlockInfo for why and how to resolve a
    /// public URL to one first.
    /// </summary>
    public PdfBuilder AddSignatureBlocks(SignatureBlockInfo left, SignatureBlockInfo right)
    {
        const double gap = 12;
        const double imageSlotHeight = 50;
        double halfWidth = (_contentWidth - gap) / 2;

        var nameFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        var labelFont = GetFont(TextStyle.Body);
        double lineHeight = labelFont.Height + 8;
        double headerHeight = GetFont(TextStyle.SmallBold).Height + 10;

        double dateLineHeight = (!string.IsNullOrEmpty(left.DateOfSignature) || !string.IsNullOrEmpty(right.DateOfSignature)) ? lineHeight : 0;

        double blockHeight = headerHeight + lineHeight + imageSlotHeight * 2 + dateLineHeight;
        EnsureSpace(blockHeight);

        double startY = _cursorY;
        double leftEndY = DrawSignatureBlock(_settings.MarginLeft, halfWidth, startY, left, headerHeight, lineHeight, imageSlotHeight, nameFont, labelFont);
        double rightEndY = DrawSignatureBlock(_settings.MarginLeft + halfWidth + gap, halfWidth, startY, right, headerHeight, lineHeight, imageSlotHeight, nameFont, labelFont);

        _cursorY = Math.Max(leftEndY, rightEndY) + 10;
        return this;
    }

    public PdfBuilder AddSignatureBlock(SignatureBlockInfo left)
    {
        const double gap = 12;
        const double imageSlotHeight = 50;
        double halfWidth = (_contentWidth - gap) / 2;

        var nameFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        var labelFont = GetFont(TextStyle.Body);
        double lineHeight = labelFont.Height + 8;
        double headerHeight = GetFont(TextStyle.SmallBold).Height + 10;

        double dateLineHeight = (!string.IsNullOrEmpty(left.DateOfSignature)) ? lineHeight : 0;

        double blockHeight = headerHeight + lineHeight + imageSlotHeight * 2 + dateLineHeight;
        EnsureSpace(blockHeight);

        double startY = _cursorY;
        double leftEndY = DrawSignatureBlock(_settings.MarginLeft, halfWidth, startY, left, headerHeight, lineHeight, imageSlotHeight, nameFont, labelFont);

        _cursorY = leftEndY + 10;
        return this;
    }

    private double DrawSignatureBlock(double x0, double width, double y0, SignatureBlockInfo info, double headerHeight, double lineHeight, double imageSlotHeight, XFont nameFont, XFont labelFont)
    {
        double y = y0;

        var headerRect = new XRect(x0, y, width, headerHeight);
        _gfx.DrawRectangle(CreateBrandGradientBrush(headerRect), headerRect);
        _gfx.DrawString(info.HeaderTitle, GetFont(TextStyle.SmallBold), XBrushes.White, new XRect(x0 + 6, y, width - 12, headerHeight), XStringFormats.CenterLeft);
        y += headerHeight;

        _gfx.DrawString(info.Name ?? "", nameFont, XBrushes.Black, new XRect(x0 + 4, y, width - 8, lineHeight), XStringFormats.CenterLeft);

        _gfx.DrawLine(XPens.LightGray, x0, y + lineHeight, x0 + width, y + lineHeight);
        y += lineHeight;

        //var z = DrawImageSlotOrLabel(x0, y, width, imageSlotHeight, info.SignatureImagePath, "Signature", labelFont, info.StampNo);
        var z = DrawImageSlotOrLabel(x0, y, width, imageSlotHeight, imagePath: null, "Signature", labelFont, stampNumber: "", slotType: ImageSlotType.Default, imageBytes: info.SignatureImage);
        var k = DrawImageSlotOrLabel(x0, y, width, imageSlotHeight, info.StampImagePath, "Stamp", labelFont, info.StampNo, ImageSlotType.Stamp);
        //var k = DrawImageSlotOrLabel1(x0, y, width, imageSlotHeight, info.StampImagePath, "Stamp", labelFont, info.StampNo, ImageSlotType.Stamp);


        if (!string.IsNullOrEmpty(info.DateOfSignature))
        {
            _gfx.DrawString($"Date: {info.DateOfSignature}", labelFont, XBrushes.Black, new XRect(x0 + 4, y, width - 8, lineHeight), XStringFormats.CenterLeft);
            y += lineHeight;
        }

        return k;
    }

    /// <summary>
    /// Draws an image centered within a fixed-height slot if the path is
    /// set and loadable; otherwise draws a plain gray placeholder label
    /// (e.g. "Signature") so the layout still looks intentional when no
    /// image is available yet.
    /// </summary>

    private double DrawImageSlotOrLabel(double x0, double y, double width, double slotHeight, string? imagePath, string placeholderLabel, XFont labelFont, string stampNumber, ImageSlotType slotType = ImageSlotType.Default)
    {
        //bool drew = false;
        double imgX, imgY = 0, w, h = 0;

        _gfx.DrawString(placeholderLabel, labelFont, XBrushes.Gray, new XRect(x0 + 30, y, width - 58, 25), slotType == ImageSlotType.Stamp ? XStringFormats.CenterLeft : XStringFormats.CenterRight);

        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                var img = XImage.FromFile(imagePath);

                if (slotType == ImageSlotType.Stamp)
                {
                    // Fixed square size, no aspect-ratio scaling
                    w = 80;
                    h = 80;
                    imgX = x0 + 10;// + (width - w) / 2;
                    imgY = y + 25;// / 2;
                }
                else
                {
                    // Default sizing behavior (signature and everything else) - scaled to fit
                    double maxW = width - 10;
                    double maxH = slotHeight - 5;
                    double scale = Math.Min(maxW / img.PixelWidth, maxH / img.PixelHeight);
                    w = img.PixelWidth * scale;
                    h = img.PixelHeight * scale;
                    imgX = x0 + (width - w) - 8;// / 2;
                    imgY = y + 25;// - (slotHeight - h) / 2;
                }


                _gfx.DrawImage(img, imgX, imgY, w, h);

                if (slotType == ImageSlotType.Stamp)
                {
                    var stampColor = XColor.FromArgb(255, 40, 40, 140); // navy/indigo, matching the reference
                    var brush = new XSolidBrush(stampColor);
                    var qaFont = new XFont("Arial", 14, XFontStyleEx.Bold);
                    _gfx.DrawString($"QA-{stampNumber}", qaFont, brush, new XRect(imgX + 20, imgY + 25, 40, 25), XStringFormats.Center);
                }
            }
            catch
            {
                // Fall through to the placeholder label below - a broken/
                // unreadable image file shouldn't blow up the whole PDF.
            }
        }

        double newY = imgY + h;
        return newY;
    }

    private double DrawImageSlotOrLabel(
    double x0, double y, double width, double slotHeight,
    string? imagePath, string placeholderLabel, XFont labelFont,
    string stampNumber, ImageSlotType slotType = ImageSlotType.Default,
    byte[]? imageBytes = null) // NEW - draw straight from binary data, no file needed
    {
        double imgX, imgY = 0, w, h = 0;

        _gfx.DrawString(placeholderLabel, labelFont, XBrushes.Gray,
            new XRect(x0 + 30, y, width - 58, 25),
            slotType == ImageSlotType.Stamp ? XStringFormats.CenterLeft : XStringFormats.CenterRight);

        bool hasBytes = imageBytes is { Length: > 0 };
        bool hasFile = !string.IsNullOrEmpty(imagePath) && File.Exists(imagePath);

        if (hasBytes || hasFile)
        {
            try
            {
                XImage img = hasBytes
                    ? XImage.FromStream(new MemoryStream(imageBytes!))
                    : XImage.FromFile(imagePath!);

                if (slotType == ImageSlotType.Stamp)
                {
                    // Fixed square size, no aspect-ratio scaling
                    w = 80;
                    h = 80;
                    imgX = x0 + 10;
                    imgY = y + 25;
                }
                else
                {
                    // Default sizing behavior (signature and everything else) - scaled to fit
                    double maxW = width - 10;
                    double maxH = slotHeight - 5;
                    double scale = Math.Min(maxW / img.PixelWidth, maxH / img.PixelHeight);
                    w = img.PixelWidth * scale;
                    h = img.PixelHeight * scale;
                    imgX = x0 + (width - w) - 8;
                    imgY = y + 25;
                }

                _gfx.DrawImage(img, imgX, imgY, w, h);

                if (slotType == ImageSlotType.Stamp)
                {
                    var stampColor = XColor.FromArgb(255, 40, 40, 140); // navy/indigo, matching the reference
                    var brush = new XSolidBrush(stampColor);
                    var qaFont = new XFont("Arial", 14, XFontStyleEx.Bold);
                    _gfx.DrawString($"QA-{stampNumber}", qaFont, brush,
                        new XRect(imgX + 20, imgY + 25, 40, 25), XStringFormats.Center);
                }
            }
            catch
            {
                // Fall through to the placeholder label above - broken/
                // unreadable image data shouldn't blow up the whole PDF.
            }
        }

        double newY = imgY + h;
        return newY;
    }

    private double DrawImageSlotOrLabel1(
    double x0, double y, double width, double slotHeight,
    string? imagePath, string placeholderLabel, XFont labelFont, string stampNumber,
    ImageSlotType slotType = ImageSlotType.Default
     ) // NEW - e.g. "04", "05", "08"
    {
        double imgX, imgY = 0, w, h = 0;

        _gfx.DrawString(placeholderLabel, labelFont, XBrushes.Gray,
            new XRect(x0 + 30, y, width - 58, 25),
            slotType == ImageSlotType.Stamp ? XStringFormats.CenterLeft : XStringFormats.CenterRight);

        // NEW: dynamic vector stamp - takes priority over imagePath when both
        // slotType is Stamp and a number is supplied, since only the number
        // changes between stamps and there's no file to load.
        if (slotType == ImageSlotType.Stamp && !string.IsNullOrEmpty(stampNumber))
        {
            double radius = 40; // matches the old fixed 80x80 stamp size (radius = size/2)
            double centerX = x0 + 10 + radius;
            double centerY = y + 25 + radius;

            DrawDynamicQaStamp(_gfx, centerX, centerY, radius, stampNumber);

            return centerY + radius; // bottom edge of the stamp circle
        }

        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                var img = XImage.FromFile(imagePath);
                if (slotType == ImageSlotType.Stamp)
                {
                    w = 80;
                    h = 80;
                    imgX = x0 + 10;
                    imgY = y + 25;
                }
                else
                {
                    double maxW = width - 10;
                    double maxH = slotHeight - 5;
                    double scale = Math.Min(maxW / img.PixelWidth, maxH / img.PixelHeight);
                    w = img.PixelWidth * scale;
                    h = img.PixelHeight * scale;
                    imgX = x0 + (width - w) - 8;
                    imgY = y + 25;
                }
                _gfx.DrawImage(img, imgX, imgY, w, h);
            }
            catch
            {
                // Fall through - a broken/unreadable image file shouldn't blow up the whole PDF.
            }
        }

        double newY = imgY + h;
        return newY;
    }

    /// <summary>
    /// Draws the circular "VISHAL MEGA MART LIMITED / QA / 04" stamp entirely
    /// with vector graphics - no image file needed, since only `number`
    /// changes between stamps. Ring text position/size is tuned for an
    /// ~80pt-diameter stamp (radius 40); pass a different radius and the
    /// proportions scale with it.
    /// </summary>
    private void DrawDynamicQaStamp1(XGraphics gfx, double centerX, double centerY, double radius, string number)
    {
        var stampColor = XColor.FromArgb(255, 20, 60, 170); // stamp-ink blue
        var pen = new XPen(stampColor, 2.0);
        var brush = new XSolidBrush(stampColor);

        // Double ring, like the reference image.
        gfx.DrawEllipse(pen, centerX - radius, centerY - radius, radius * 2, radius * 2);
        gfx.DrawEllipse(pen, centerX - radius + 4, centerY - radius + 4, (radius - 4) * 2, (radius - 4) * 2);

        var ringFont = new XFont("Arial", radius * 0.19, XFontStyleEx.Bold);
        double textRadius = radius - 12;

        // Text wraps ~300° around the ring, leaving a ~60° gap at the bottom
        // for the star - matches the reference layout.
        DrawTextOnArc(gfx, "VISHAL MEGA MART LIMITED", centerX, centerY, textRadius,
            ringFont, brush, startAngleDeg: 120, sweepDeg: 300, clockwise: true);

        DrawStar(gfx, centerX, centerY + radius - 14, 7, stampColor);

        var qaFont = new XFont("Arial", radius * 0.34, XFontStyleEx.BoldItalic);
        gfx.DrawString("QA", qaFont, brush,
            new XRect(centerX - radius, centerY - radius * 0.42, radius * 2, radius * 0.5),
            XStringFormats.Center);

        var numberFont = new XFont("Arial", radius * 0.34, XFontStyleEx.Bold);
        gfx.DrawString(number, numberFont, brush,
            new XRect(centerX - radius, centerY - radius * 0.05, radius * 2, radius * 0.5),
            XStringFormats.Center);
    }

    /// <summary>
    /// Draws the "VISHAL MEGA MART LIMITED / QA-4 / INSPECTED" stamp variant:
    /// a top text arc, a bottom text arc, stars at the left/right cardinal
    /// points, and a bordered box with "QA-{number}" centered inside.
    /// </summary>
    private void DrawDynamicQaStamp(
        XGraphics gfx, double centerX, double centerY, double radius, string number,
        string topText = "* VISHAL MEGA MART LIMITED *",
        string bottomText = "INSPECTED",
        string centerLabel = "QA")
    {
        var stampColor = XColor.FromArgb(255, 40, 40, 140); // navy/indigo, matching the reference
        var ringPen = new XPen(stampColor, 2.0);
        var thinPen = new XPen(stampColor, 1.2);
        var AltrathinPen = new XPen(stampColor, .8);
        var AltraXthinPen = new XPen(stampColor, .5);
        var brush = new XSolidBrush(stampColor);

        // Double outer ring
        gfx.DrawEllipse(ringPen, centerX - radius, centerY - radius, radius * 2, radius * 2);
        gfx.DrawEllipse(thinPen, centerX - radius + 2, centerY - radius + 2, (radius - 2) * 2, (radius - 2) * 2);


        gfx.DrawEllipse(AltrathinPen, centerX - radius + 14, centerY - radius + 14, (radius - 14) * 2, (radius - 14) * 2);
        gfx.DrawEllipse(AltraXthinPen, centerX - radius + 13, centerY - radius + 13, (radius - 13) * 2, (radius - 13) * 2);

        double textRadius = radius - 10;
        var ringFont = new XFont("Arial", 5, XFontStyleEx.Bold);
        var ringINSPFont = new XFont("Arial", 6, XFontStyleEx.Bold);

        // Top arc, centered at the top of the circle (270° in our y-down
        // convention - see the note on DrawTextOnArc from before).
        DrawTextOnArc(gfx, topText, centerX, centerY, textRadius, ringFont, brush, startAngleDeg: 150, sweepDeg: 240, clockwise: true);

        // Bottom arc, centered at the bottom (90°).

        DrawTextOnArc(gfx, bottomText, centerX, centerY, textRadius + 5, ringINSPFont, brush, startAngleDeg: 45, sweepDeg: 100, clockwise: true, flipForBottomArc: true);

        // Two stars at the exact left (180°) and right (0°) points, between
        // the two text arcs - not one star at the bottom like before.
        double starRadius = radius - 6;// radius * 0.68;
        double starSize = 3;// radius * 0.10;
        //DrawStar(gfx, centerX + starRadius * Math.Cos(Math.PI), centerY + starRadius * Math.Sin(Math.PI), starSize, stampColor);
        //DrawStar(gfx, centerX + starRadius * Math.Cos(0), centerY + starRadius * Math.Sin(0), starSize, stampColor);

        // Right side (0°):
        //DrawStar(gfx, centerX + starRadius * Math.Cos(0), centerY + starRadius * Math.Sin(0), starSize, stampColor);

        // Bottom (90°):
        //DrawStar(gfx, centerX - 1 + starRadius * Math.Cos(120), centerY + 2 + textRadius * Math.Sin(120), starSize, stampColor);
        //DrawStar(gfx, centerX - 70 + starRadius * Math.Cos(120), centerY + 2 + textRadius * Math.Sin(120), starSize, stampColor);

        // Top (270°, or equivalently -90°):
        //DrawStar(gfx, centerX + starRadius * Math.Cos(-Math.PI / 2), centerY + starRadius * Math.Sin(-Math.PI / 2), starSize, stampColor);

        // Center box: two horizontal border lines framing "QA-{number}"
        double boxHalfWidth = radius * 0.59;
        double boxHalfHeight = radius * 0.27;
        gfx.DrawLine(thinPen, centerX - boxHalfWidth, centerY - boxHalfHeight, centerX + boxHalfWidth, centerY - boxHalfHeight);
        gfx.DrawLine(thinPen, centerX - boxHalfWidth, centerY + boxHalfHeight, centerX + boxHalfWidth, centerY + boxHalfHeight);

        var qaFont = new XFont("Arial", radius * 0.35, XFontStyleEx.BoldItalic);
        gfx.DrawString($"{centerLabel}-{number}", qaFont, brush,
            new XRect(centerX - boxHalfWidth, centerY - boxHalfHeight, boxHalfWidth * 2, boxHalfHeight * 2),
            XStringFormats.Center);
    }

    /// <summary>
    /// Draws `text` curved along a circular arc, one character at a time,
    /// each rotated so its baseline is tangent to the circle. PdfSharp has no
    /// built-in text-on-a-path support, so this measures each glyph and
    /// distributes them proportionally to actual glyph width (not just equal
    /// angular spacing) so the spacing looks even regardless of character width.
    ///
    /// NOTE: I couldn't render/preview this in the sandbox that wrote it -
    /// if the text comes out upside-down, mirrored, or running the wrong way
    /// around the circle, flip `clockwise` first; if orientation is still off,
    /// the fix is changing the "+ 90" in the rotation formula below to "- 90".
    /// </summary>

    private void DrawTextOnArc(
     XGraphics gfx, string text, double cx, double cy, double radius,
     XFont font, XBrush brush, double startAngleDeg, double sweepDeg,
     bool clockwise = true, double letterSpacingPoints = 0,
     bool flipForBottomArc = false) // NEW
    {
        // Text sitting on the bottom half of the ring needs BOTH the character
        // order reversed AND the rotation flipped 180° compared to the top-arc
        // convention, to read correctly left-to-right with upright glyphs -
        // see the explanation in chat for why one flip alone isn't enough.
        string effectiveText = flipForBottomArc ? new string(text.Reverse().ToArray()) : text;
        double rotationOffset = flipForBottomArc ? -90 : 90;

        var charWidths = effectiveText.Select(c => gfx.MeasureString(c.ToString(), font).Width).ToArray();
        double totalCharWidth = charWidths.Sum();
        double totalSpacingWidth = letterSpacingPoints * (effectiveText.Length - 1);
        double totalWidth = totalCharWidth + totalSpacingWidth;
        if (totalWidth <= 0) return;

        double sweepRad = (sweepDeg * Math.PI / 180.0) * (clockwise ? 1 : -1);
        double startAngleRad = startAngleDeg * Math.PI / 180.0;
        double radiansPerPoint = sweepRad / totalWidth;
        double angleAccum = 0;

        for (int i = 0; i < effectiveText.Length; i++)
        {
            double charAngle = charWidths[i] * radiansPerPoint;
            double angle = startAngleRad + angleAccum + charAngle / 2;

            double x = cx + radius * Math.Cos(angle);
            double y = cy + radius * Math.Sin(angle);

            var state = gfx.Save();
            gfx.TranslateTransform(x, y);
            gfx.RotateTransform(angle * 180.0 / Math.PI + rotationOffset);
            gfx.DrawString(effectiveText[i].ToString(), font, brush, new XPoint(0, 0));
            gfx.Restore(state);

            double spacingAngle = letterSpacingPoints * radiansPerPoint;
            angleAccum += charAngle + spacingAngle;
        }
    }
    /// <summary>Draws a 5-pointed star outline centered at (cx, cy).</summary>
    private void DrawStar(XGraphics gfx, double cx, double cy, double outerRadius, XColor color)
    {
        var pen = new XPen(color, 0.9);
        double innerRadius = outerRadius * 0.42;
        var points = new XPoint[10];

        for (int i = 0; i < 10; i++)
        {
            double angle = -Math.PI / 2 + i * Math.PI / 5; // start pointing straight up
            double r = (i % 2 == 0) ? outerRadius : innerRadius;
            points[i] = new XPoint(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
        }

        gfx.DrawPolygon(pen, points);
    }

    /// <summary>
    /// Draws several label/value fields on a single line, proportioned by
    /// weight - mirrors a MudGrid row built from several adjacent
    /// label/value MudItem pairs (e.g. "Season: X   Color Code: Y").
    /// </summary>

    public PdfBuilder AddFieldsRow(params RowField[] fields)
    {
        var boldFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        var font = GetFont(TextStyle.Body);
        double singleLineHeight = font.Height + 4;//

        double totalWeight = fields.Sum(f => f.Weight);

        // First pass: compute each field's geometry and wrap its label/value
        // text within their allotted widths, so long text wraps onto extra
        // lines instead of overflowing into the next field's column.
        var layout = new List<(double X, double Width, double LabelWidth, List<string> LabelLines, List<string> ValueLines)>();
        double x = _settings.MarginLeft;
        int maxLines = 1;

        foreach (var field in fields)
        {
            double width = _contentWidth * (field.Weight / totalWeight);
            double labelWidth = 150;// Math.Min(width * 0.45, 160);
            double valueWidth = width - labelWidth;

            var labelLines = WrapText(field.Label, boldFont, labelWidth - 4);
            var valueLines = WrapText(field.Value ?? "", font, valueWidth - 4);

            maxLines = Math.Max(maxLines, Math.Max(labelLines.Count, valueLines.Count));
            layout.Add((x, width, labelWidth, labelLines, valueLines));

            x += width;
        }

        double rowHeight = singleLineHeight * maxLines;
        EnsureSpace(rowHeight);

        // Second pass: draw each field's wrapped lines at the (now known) cursor Y.
        foreach (var field in layout)
        {
            for (int i = 0; i < field.LabelLines.Count; i++)
            {
                _gfx.DrawString(field.LabelLines[i], boldFont, XBrushes.Black,
                    new XRect(field.X, _cursorY + i * singleLineHeight, field.LabelWidth, singleLineHeight),
                    XStringFormats.CenterLeft);
            }

            for (int i = 0; i < field.ValueLines.Count; i++)
            {
                _gfx.DrawString(field.ValueLines[i], font, XBrushes.Black,
                    new XRect(field.X + field.LabelWidth, _cursorY + i * singleLineHeight,
                        field.Width - field.LabelWidth, singleLineHeight),
                    XStringFormats.CenterLeft);
            }
        }

        XPen DarkMagenta = new XPen(XColors.LightGray, 0.10);
        _gfx.DrawLine(DarkMagenta, _settings.MarginLeft, _cursorY + rowHeight, _settings.MarginLeft + _contentWidth, _cursorY + rowHeight);

        _cursorY += rowHeight;
        return this;
    }

    /// <summary>
    /// A full-width label with a wrapped, possibly multi-line value below
    /// or beside it - use for long fields like "Article Number" or
    /// "Area of fail GPTR" that can't fit on one line at fixed width.
    /// </summary>
    public PdfBuilder AddWrappedKeyValue(string key, string value, double keyWidth = 150)
    {
        var boldFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        var font = GetFont(TextStyle.Body);
        double valueWidth = _contentWidth - keyWidth;
        var lines = WrapText(value, font, valueWidth);
        double lineHeight = font.Height + 4;//

        EnsureSpace(lineHeight * lines.Count);

        _gfx.DrawString(key, boldFont, XBrushes.Black, new XRect(_settings.MarginLeft, _cursorY, keyWidth, lineHeight), XStringFormats.CenterLeft);

        double y = _cursorY;
        foreach (var line in lines)
        {
            _gfx.DrawString(line, font, XBrushes.Black, new XRect(_settings.MarginLeft + keyWidth, y, valueWidth, lineHeight), XStringFormats.CenterLeft);
            y += lineHeight;
        }

        XPen DarkMagenta = new XPen(XColors.LightGray, 0.10);
        _gfx.DrawLine(DarkMagenta, _settings.MarginLeft, _cursorY + (lineHeight * lines.Count), _settings.MarginLeft + _contentWidth, _cursorY + (lineHeight * lines.Count));

        _cursorY = y;//+ 4
        return this;
    }

    /// <summary>
    /// Draws two independent tables side by side (left half / right half
    /// of the content width) - mirrors the two MudItem xs="6" tables in
    /// the Conformity section. Does not support a page break mid-table;
    /// fine for the small (5-10 row) tables this is meant for.
    /// </summary>
    public PdfBuilder AddSideBySideTables(TableSpec left, TableSpec right)
    {
        double gap = 12;
        double halfWidth = (_contentWidth - gap) / 2;

        double leftHeight = EstimateTableHeight(left);
        double rightHeight = EstimateTableHeight(right);
        EnsureSpace(Math.Max(leftHeight, rightHeight));

        double startY = _cursorY;
        double leftEndY = DrawTableBlock(_settings.MarginLeft, halfWidth, startY, left);
        double rightEndY = DrawTableBlock(_settings.MarginLeft + halfWidth + gap, halfWidth, startY, right);

        _cursorY = Math.Max(leftEndY, rightEndY) + 10;
        return this;
    }
    private double EstimateTableHeight(TableSpec spec)
    {
        var bodyFont = GetFont(TextStyle.Body);
        double rowHeight = bodyFont.Height + 10;
        double titleHeight = string.IsNullOrEmpty(spec.Title) ? 0 : (GetFont(TextStyle.SubHeading).Height + 8);
        return titleHeight + rowHeight * (spec.Rows.Count + 1); // +1 for header row
    }

    private double DrawTableBlock(double x0, double width, double y0, TableSpec spec)
    {
        double y = y0;

        if (!string.IsNullOrEmpty(spec.Title))
        {
            var titleFont = GetFont(TextStyle.SubHeading);
            _gfx.DrawString(spec.Title, titleFont, XBrushes.Black,
                new XRect(x0, y, width, titleFont.Height + 4), XStringFormats.CenterLeft);
            y += titleFont.Height + 8;
        }

        var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
        var bodyFont = GetFont(TextStyle.Body);
        double singleLineHeight = bodyFont.Height + 10;

        if (!string.IsNullOrEmpty(spec.ColSpanHeaderText))
        {
            var colSpanHeaderRect = new XRect(x0, y, width, singleLineHeight);
            _gfx.DrawRectangle(BrandBrush, colSpanHeaderRect);
            _gfx.DrawString(spec.ColSpanHeaderText, headerFont, XBrushes.White,
                new XRect(x0 + 4, y, width - 8, singleLineHeight), XStringFormats.CenterLeft);
            y += singleLineHeight;
        }

        double totalWeight = spec.Columns.Sum(c => c.Weight);
        double[] colWidths = spec.Columns.Select(c => width * (c.Weight / totalWeight)).ToArray();

        // A table can be "banner-only" (like "No. of Check Carton/Bags and
        // Pre-Pack", whose original HTML had a colspan <th> and nothing
        // else in <thead>) - if every column header is blank, skip drawing
        // a second, empty magenta strip beneath the banner.
        bool hasColumnHeaders = spec.Columns.Any(c => !string.IsNullOrWhiteSpace(c.Header));

        double x = x0;
        if (hasColumnHeaders)
        {
            var headerRect = new XRect(x0, y, width, singleLineHeight);
            _gfx.DrawRectangle(BrandBrush, headerRect);
            for (int i = 0; i < spec.Columns.Count; i++)
            {
                var format = ToFormat(spec.Columns[i].Align);
                _gfx.DrawString(spec.Columns[i].Header, headerFont, XBrushes.White,
                    new XRect(x + 4, y, colWidths[i] - 8, singleLineHeight), format);
                x += colWidths[i];
            }
            y += singleLineHeight;
        }

        var boldBodyFont = new XFont("Arial", 10, XFontStyleEx.Bold);


        for (int rowIndex = 0; rowIndex < spec.Rows.Count; rowIndex++)
        {
            var row = spec.Rows[rowIndex];
            bool isBoldRow = spec.BoldRows?.Contains(rowIndex) ?? false;
            var rowFont = isBoldRow ? boldBodyFont : bodyFont;

            // Wrap each non-chip cell within its column width so the row
            // grows to fit the tallest cell instead of overflowing. Bold
            // rows never render as chips, even if the column is in
            // ChipColumns - e.g. a "WITHIN TOLERANCE / PASS-FAIL" label
            // row should stay plain bold text, not become a status pill.
            var cellLines = new List<string>[spec.Columns.Count];
            int maxLines = 1;
            for (int i = 0; i < spec.Columns.Count && i < row.Count; i++)
            {
                bool asChip = !isBoldRow && (spec.ChipColumns?.Contains(i) ?? false);
                if (asChip)
                {
                    cellLines[i] = new List<string> { row[i] ?? "" };
                }
                else
                {
                    cellLines[i] = WrapText(row[i] ?? "", rowFont, colWidths[i] - 8);
                    maxLines = Math.Max(maxLines, cellLines[i].Count);
                }
            }

            double rowHeight = singleLineHeight * maxLines;

            x = x0;
            for (int i = 0; i < spec.Columns.Count && i < row.Count; i++)
            {
                bool asChip = !isBoldRow && (spec.ChipColumns?.Contains(i) ?? false);
                if (asChip)
                {
                    DrawCellChip(x, colWidths[i], y, rowHeight, row[i] ?? "", spec.Columns[i].Align);
                }
                else
                {
                    var format = ToFormat(spec.Columns[i].Align);
                    var lines = cellLines[i] ?? new List<string> { "" };
                    double verticalOffset = (rowHeight - singleLineHeight * lines.Count) / 2;
                    for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
                    {
                        _gfx.DrawString(lines[lineIdx], rowFont, XBrushes.Black,
                            new XRect(x + 4, y + verticalOffset + lineIdx * singleLineHeight, colWidths[i] - 8, singleLineHeight), format);
                    }
                }
                x += colWidths[i];
            }
            _gfx.DrawLine(XPens.LightGray, x0, y + rowHeight, x0 + width, y + rowHeight);
            y += rowHeight;
        }
        return y;
    }

    /// <summary>
    /// Marks that "Page X of Y" footers should be stamped on every page.
    /// Drawn in Save()/ToBytes(), since total page count is only known
    /// once all content has been added. Footers are only stamped on the
    /// report's own pages - not on any pages added via AddAttachment(s).
    /// </summary>
    public PdfBuilder WithPageNumbers()
    {
        _footerPending = true;
        return this;
    }

    private int _contentPageCount = -1;

    /// <summary>
    /// Appends one attachment at the end of the document, in call order.
    /// PDFs have their pages merged in directly; images are placed on
    /// their own page, scaled to fit within the margins.
    ///
    /// IMPORTANT: call this (or AddAttachments) LAST - after all other
    /// Add*/WithPageNumbers() calls and right before Save()/ToBytes().
    /// Attachment pages are always appended at the end of the document
    /// regardless of when you call this, so calling it mid-report would
    /// put report content after the attachments instead of before.
    /// </summary>
    public PdfBuilder AddAttachment(string filePath, string? caption = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Attachment not found: {filePath}", filePath);
        }

        // First call captures how many pages belong to the report itself,
        // so page-number footers don't get stamped onto attachment pages.
        if (_contentPageCount < 0)
        {
            _contentPageCount = _document.PageCount;
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".pdf")
        {
            using var externalDoc = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            foreach (var page in externalDoc.Pages)
            {
                _document.AddPage(page);
            }

        }
        else
        {
            var page = _document.AddPage();
            ApplyPageSize(page);
            using var gfx = XGraphics.FromPdfPage(page);

            var img = XImage.FromFile(filePath);
            double maxW = page.Width.Point - _settings.MarginLeft - _settings.MarginRight;
            double maxH = page.Height.Point - _settings.MarginTop - _settings.MarginBottom
                          - (caption is null ? 0 : 20);

            double scale = Math.Min(maxW / img.PixelWidth, maxH / img.PixelHeight);
            double w = img.PixelWidth * scale;
            double h = img.PixelHeight * scale;
            double x = _settings.MarginLeft + (maxW - w) / 2;
            double y = _settings.MarginTop;
            gfx.DrawImage(img, x, y, w, h);
            if (caption is not null)
            {
                gfx.DrawString(caption, GetFont(TextStyle.Small), XBrushes.Gray,
                    new XRect(_settings.MarginLeft, y + h + 6, maxW, 16), XStringFormats.TopCenter);
            }
        }

        return this;
    }

    /// <summary>Convenience wrapper to add several attachments in order.</summary>
    public PdfBuilder AddAttachments(IEnumerable<AttachmentFile> attachments)
    {
        foreach (var a in attachments)
        {
            AddAttachment(a.FilePath, a.Caption);
        }
        return this;
    }

    private bool _gfxFinalized;
    private void FinalizeContentGraphics()
    {
        if (_gfxFinalized) return;
        _gfxFinalized = true;
        _gfx.Dispose();
    }

    //private void ApplyFootersIfNeeded()
    //{
    //    if (!_footerPending || _footersApplied) return;
    //    _footersApplied = true;

    //    FinalizeContentGraphics();

    //    // Only stamp footers on the report's own pages, not on anything
    //    // appended afterward via AddAttachment(s).
    //    int total = _contentPageCount >= 0 ? _contentPageCount : _document.PageCount;
    //    for (int i = 0; i < total; i++)
    //    {
    //        var page = _document.Pages[i];
    //        using var gfx = XGraphics.FromPdfPage(page);
    //        var text = $"{_footerTitle}  |  Page {i + 1} of {total}";
    //        gfx.DrawString(text, GetFont(TextStyle.SmallBold), XBrushes.Black, new XRect(_settings.MarginLeft, page.Height.Point - 30, _contentWidth, 16), XStringFormats.TopCenter);
    //    }
    //}
    private void ApplyFootersIfNeeded()
    {
        if (!_footerPending || _footersApplied) return;
        _footersApplied = true;
        FinalizeContentGraphics();

        int total = _contentPageCount >= 0 ? _contentPageCount : _document.PageCount;

        for (int i = 0; i < total; i++)
        {
            var page = _document.Pages[i];
            using var gfx = XGraphics.FromPdfPage(page);
            var font = GetFont(TextStyle.Small);

            // Build footer lines: your title (possibly multi-line) + page number line
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(_footerTitle))
            {
                // supports explicit \n in _footerTitle, or wrap long text
                lines.AddRange(_footerTitle.Split('\n'));
            }
            lines.Add($"Page {i + 1} of {total}");

            double lineHeight = font.GetHeight() + 2; // tweak spacing as needed
            double blockHeight = lines.Count * lineHeight;
            double startY = page.Height.Point - blockHeight - 10; // 10pt bottom padding

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                var y = startY + (lineIndex * lineHeight);
                gfx.DrawString(
                    lines[lineIndex],
                    font,
                    XBrushes.Black,
                    new XRect(_settings.MarginLeft, y, _contentWidth, lineHeight),
                    XStringFormats.TopCenter);
            }
        }
    }

    private string? _watermarkText;
    private double _watermarkOpacity = 0.15;
    private bool _watermarkApplied;

    /// <summary>
    /// Draws diagonal, semi-transparent watermark text (e.g. "DRAFT",
    /// "CONFIDENTIAL", "SAMPLE") across every page of the report. Applied
    /// in Save()/ToBytes(), same as page numbers - only covers the
    /// report's own pages, not anything appended via AddAttachment(s).
    /// </summary>
    /// <param name="text">Watermark text.</param>
    /// <param name="opacity">0.0 (invisible) to 1.0 (solid) - default 0.15 keeps content readable underneath.</param>
    public PdfBuilder WithWatermark(string text, double opacity = 0.15)
    {
        _watermarkText = text;
        _watermarkOpacity = Math.Clamp(opacity, 0.0, 1.0);
        return this;
    }
    private void ApplyWatermarkIfNeeded()
    {
        if (string.IsNullOrEmpty(_watermarkText) || _watermarkApplied) return;
        _watermarkApplied = true;
        FinalizeContentGraphics();

        int total = _contentPageCount >= 0 ? _contentPageCount : _document.PageCount;
        var font = new XFont("Arial", 54, XFontStyleEx.Bold);
        int alpha = (int)Math.Round(255 * _watermarkOpacity);
        var brush = new XSolidBrush(XColor.FromArgb(alpha, 128, 128, 128));

        var lines = _watermarkText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) lines = new[] { _watermarkText };

        double lineHeight = font.Height + 6;
        double blockHeight = lineHeight * lines.Length;

        for (int i = 0; i < total; i++)
        {
            var page = _document.Pages[i];
            using var gfx = XGraphics.FromPdfPage(page);

            double centerX = page.Width.Point / 2;
            double centerY = page.Height.Point / 2;

            // Widest line determines the block width, so every line can
            // be centered horizontally within the same box.
            double blockWidth = lines.Max(l => gfx.MeasureString(l, font).Width) + 20;

            // Rotate about the page center so the text block runs
            // diagonally corner-to-corner, then draw each line centered
            // within its own slice of the vertically-stacked block.
            var state = gfx.Save();
            gfx.TranslateTransform(centerX, centerY);
            gfx.RotateTransform(-40);

            double top = -blockHeight / 2;
            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                var lineRect = new XRect(-blockWidth / 2, top + lineIdx * lineHeight, blockWidth, lineHeight);
                gfx.DrawString(lines[lineIdx], font, brush, lineRect, XStringFormats.Center);
            }

            gfx.Restore(state);
        }
    }

    private List<string> WrapText(string text, XFont font, double maxWidth)
    {
        var lines = new List<string>();
        if (text != null)
        {
            var words = text.Split(' ');

            var current = "";

            foreach (var word in words)
            {
                var trial = current.Length == 0 ? word : current + " " + word;
                var size = _gfx.MeasureString(trial, font);
                if (size.Width > maxWidth && current.Length > 0)
                {
                    lines.Add(current);
                    current = word;
                }
                else
                {
                    current = trial;
                }
            }
            if (current.Length > 0) lines.Add(current);
            if (lines.Count == 0) lines.Add("");
        }
        return lines;
    }

    public void Save(string path)
    {
        ApplyWatermarkIfNeeded();
        ApplyFootersIfNeeded();
        _document.Save(path);
    }

    public byte[] ToBytes()
    {
        ApplyWatermarkIfNeeded();
        ApplyFootersIfNeeded();
        using var ms = new MemoryStream();
        _document.Save(ms, false);
        return ms.ToArray();
    }

    public void Dispose()
    {
        if (!_gfxFinalized) _gfx.Dispose();
        _document.Dispose();
    }
}
