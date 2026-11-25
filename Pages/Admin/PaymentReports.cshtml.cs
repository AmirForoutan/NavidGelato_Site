using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages.Admin;

public sealed class PaymentReportsModel(AppDbContext db, IAdminPermissionService permissions) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ReportFilter Filter { get; set; } = new();

    public List<ReportRow> Items { get; private set; } = [];
    public List<BranchOption> Branches { get; private set; } = [];
    public ReportSummary Summary { get; private set; } = new();
    public int TotalRows { get; private set; }
    public int TotalPages { get; private set; } = 1;

    public sealed class ReportFilter
    {
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
        public int? BranchId { get; set; }
        public PaymentLinkStatus? Status { get; set; }
        public bool? Opened { get; set; }
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public sealed record BranchOption(int Id, string Name);

    public sealed class ReportRow
    {
        public long Id { get; init; }
        public int BranchId { get; init; }
        public string BranchName { get; init; } = string.Empty;
        public string PublicToken { get; init; } = string.Empty;
        public string ResNum { get; init; } = string.Empty;
        public long Amount { get; init; }
        public string SettlementIban { get; init; } = string.Empty;
        public string? CellNumber { get; init; }
        public PaymentLinkStatus Status { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime? OpenedAtUtc { get; init; }
        public DateTime? ExpiresAtUtc { get; init; }
        public DateTime? PaidAtUtc { get; init; }
        public DateTime? LastCallbackAtUtc { get; init; }
        public bool HasGatewayToken { get; init; }
        public string? GatewayRequestId { get; init; }
        public DateTime? GatewayRequestStartedAtUtc { get; init; }
        public string? RefNum { get; init; }
        public string? TraceNo { get; init; }
        public long? VerifiedAmount { get; init; }
        public string? FailureMessage { get; init; }
    }

    public sealed class ReportSummary
    {
        public int TotalRequests { get; init; }
        public long TotalAmount { get; init; }
        public int PaidCount { get; init; }
        public long PaidAmount { get; init; }
        public int FailedCount { get; init; }
        public int ExpiredCount { get; init; }
        public int PendingCount { get; init; }
        public int OpenedCount { get; init; }
        public int UnopenedCount { get; init; }
        public int VerifyingCount { get; init; }
        public decimal SuccessRate => TotalRequests == 0 ? 0 : Math.Round(PaidCount * 100m / TotalRequests, 1);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.PaymentReports.View))
            return Forbid();

        NormalizeFilterDates();
        await LoadBranchesAsync();

        if (!TryGetUtcRange(out var fromUtc, out var toUtcExclusive))
        {
            Items = [];
            Summary = new ReportSummary();
            return Page();
        }

        var nowUtc = DateTime.UtcNow;
        var scope = BuildScopeQuery(fromUtc, toUtcExclusive);

        Summary = new ReportSummary
        {
            TotalRequests = await scope.CountAsync(),
            TotalAmount = await scope.SumAsync(x => (long?)x.Amount) ?? 0,
            PaidCount = await scope.CountAsync(x => x.Status == PaymentLinkStatus.Paid),
            PaidAmount = await scope.Where(x => x.Status == PaymentLinkStatus.Paid).SumAsync(x => (long?)x.Amount) ?? 0,
            FailedCount = await scope.CountAsync(x => x.Status == PaymentLinkStatus.Failed),
            ExpiredCount = await scope.CountAsync(x =>
                x.Status == PaymentLinkStatus.Expired ||
                (x.Status != PaymentLinkStatus.Paid &&
                 x.Status != PaymentLinkStatus.Failed &&
                 x.ExpiresAtUtc.HasValue &&
                 x.ExpiresAtUtc.Value <= nowUtc)),
            PendingCount = await scope.CountAsync(x =>
                x.Status != PaymentLinkStatus.Paid &&
                x.Status != PaymentLinkStatus.Failed &&
                x.Status != PaymentLinkStatus.Expired &&
                (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc.Value > nowUtc)),
            OpenedCount = await scope.CountAsync(x => x.OpenedAtUtc.HasValue),
            UnopenedCount = await scope.CountAsync(x => !x.OpenedAtUtc.HasValue),
            VerifyingCount = await scope.CountAsync(x => x.Status == PaymentLinkStatus.VerificationPending)
        };

        var query = ApplyStatusFilter(scope, nowUtc);
        TotalRows = await query.CountAsync();

        Filter.PageSize = Filter.PageSize is 25 or 50 or 100 ? Filter.PageSize : 50;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRows / (double)Filter.PageSize));
        Filter.PageNumber = Math.Clamp(Filter.PageNumber, 1, TotalPages);

        Items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((Filter.PageNumber - 1) * Filter.PageSize)
            .Take(Filter.PageSize)
            .Select(x => new ReportRow
            {
                Id = x.Id,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : "-",
                PublicToken = x.PublicToken,
                ResNum = x.ResNum,
                Amount = x.Amount,
                SettlementIban = x.SettlementIban,
                CellNumber = x.CellNumber,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc,
                OpenedAtUtc = x.OpenedAtUtc,
                ExpiresAtUtc = x.ExpiresAtUtc,
                PaidAtUtc = x.PaidAtUtc,
                LastCallbackAtUtc = x.LastCallbackAtUtc,
                HasGatewayToken = x.GatewayToken != null && x.GatewayToken != "",
                GatewayRequestId = x.GatewayRequestId,
                GatewayRequestStartedAtUtc = x.GatewayRequestStartedAtUtc,
                RefNum = x.RefNum,
                TraceNo = x.TraceNo,
                VerifiedAmount = x.VerifiedAmount,
                FailureMessage = x.FailureMessage
            })
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.PaymentReports.View) ||
            !await permissions.HasAsync(User, AdminPermissions.PaymentReports.Export))
            return Forbid();

        NormalizeFilterDates();

        if (!TryGetUtcRange(out var fromUtc, out var toUtcExclusive))
            return BadRequest("بازه تاریخ معتبر نیست.");

        var canViewDetails = await permissions.HasAsync(User, AdminPermissions.PaymentReports.ViewDetails);
        var query = ApplyStatusFilter(BuildScopeQuery(fromUtc, toUtcExclusive), DateTime.UtcNow);

        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new ReportRow
            {
                Id = x.Id,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : "-",
                PublicToken = x.PublicToken,
                ResNum = x.ResNum,
                Amount = x.Amount,
                SettlementIban = x.SettlementIban,
                CellNumber = x.CellNumber,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc,
                OpenedAtUtc = x.OpenedAtUtc,
                ExpiresAtUtc = x.ExpiresAtUtc,
                PaidAtUtc = x.PaidAtUtc,
                LastCallbackAtUtc = x.LastCallbackAtUtc,
                HasGatewayToken = x.GatewayToken != null && x.GatewayToken != "",
                GatewayRequestId = x.GatewayRequestId,
                GatewayRequestStartedAtUtc = x.GatewayRequestStartedAtUtc,
                RefNum = x.RefNum,
                TraceNo = x.TraceNo,
                VerifiedAmount = x.VerifiedAmount,
                FailureMessage = x.FailureMessage
            })
            .ToListAsync();

        var workbook = BuildExcelWorkbook(rows, canViewDetails);

        return File(
            workbook,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"payment-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }

    private byte[] BuildExcelWorkbook(IReadOnlyList<ReportRow> rows, bool canViewDetails)
    {
        var headers = canViewDetails
            ? new[]
            {
                "Id", "شعبه", "وضعیت", "مبلغ (ریال)", "موبایل", "زمان ایجاد",
                "زمان باز شدن", "زمان انقضا", "زمان پرداخت", "ResNum", "RefNum",
                "TraceNo", "مبلغ تاییدشده (ریال)", "شبا تسویه", "آخرین Callback", "خطا"
            }
            : new[]
            {
                "Id", "شعبه", "وضعیت", "مبلغ (ریال)", "موبایل", "زمان ایجاد",
                "زمان باز شدن", "زمان انقضا", "زمان پرداخت"
            };

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteContentTypes(archive);
            WriteRootRelationships(archive);
            WriteWorkbook(archive);
            WriteWorkbookRelationships(archive);
            WriteStyles(archive);
            WriteWorksheet(archive, rows, headers, canViewDetails);
        }

        return output.ToArray();
    }

    private void WriteWorksheet(
        ZipArchive archive,
        IReadOnlyList<ReportRow> rows,
        IReadOnlyList<string> headers,
        bool canViewDetails)
    {
        WriteXmlEntry(archive, "xl/worksheets/sheet1.xml", writer =>
        {
            const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            writer.WriteStartElement("worksheet", ns);

            var lastColumn = GetExcelColumnName(headers.Count);
            var lastRow = Math.Max(1, rows.Count + 1);
            writer.WriteStartElement("dimension", ns);
            writer.WriteAttributeString("ref", $"A1:{lastColumn}{lastRow}");
            writer.WriteEndElement();

            writer.WriteStartElement("sheetViews", ns);
            writer.WriteStartElement("sheetView", ns);
            writer.WriteAttributeString("workbookViewId", "0");
            writer.WriteAttributeString("rightToLeft", "1");
            writer.WriteStartElement("pane", ns);
            writer.WriteAttributeString("ySplit", "1");
            writer.WriteAttributeString("topLeftCell", "A2");
            writer.WriteAttributeString("activePane", "bottomLeft");
            writer.WriteAttributeString("state", "frozen");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("sheetFormatPr", ns);
            writer.WriteAttributeString("defaultRowHeight", "18");
            writer.WriteEndElement();

            var widths = canViewDetails
                ? new[] { 10d, 24d, 22d, 18d, 17d, 22d, 22d, 22d, 22d, 26d, 28d, 22d, 22d, 32d, 22d, 48d }
                : new[] { 10d, 24d, 22d, 18d, 17d, 22d, 22d, 22d, 22d };

            writer.WriteStartElement("cols", ns);
            for (var i = 0; i < widths.Length; i++)
            {
                writer.WriteStartElement("col", ns);
                writer.WriteAttributeString("min", (i + 1).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("max", (i + 1).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("width", widths[i].ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("customWidth", "1");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("sheetData", ns);

            writer.WriteStartElement("row", ns);
            writer.WriteAttributeString("r", "1");
            writer.WriteAttributeString("ht", "24");
            writer.WriteAttributeString("customHeight", "1");
            for (var i = 0; i < headers.Count; i++)
                WriteTextCell(writer, i + 1, 1, headers[i], styleIndex: 1);
            writer.WriteEndElement();

            var excelRow = 2;
            foreach (var row in rows)
            {
                writer.WriteStartElement("row", ns);
                writer.WriteAttributeString("r", excelRow.ToString(CultureInfo.InvariantCulture));

                var column = 1;
                WriteNumberCell(writer, column++, excelRow, row.Id);
                WriteTextCell(writer, column++, excelRow, row.BranchName);
                WriteTextCell(writer, column++, excelRow, GetStatusTitle(GetEffectiveStatus(row)));
                WriteNumberCell(writer, column++, excelRow, row.Amount, styleIndex: 2);
                WriteTextCell(writer, column++, excelRow, row.CellNumber);
                WriteTextCell(writer, column++, excelRow, FormatDateTime(row.CreatedAtUtc));
                WriteTextCell(writer, column++, excelRow, FormatDateTime(row.OpenedAtUtc));
                WriteTextCell(writer, column++, excelRow, FormatDateTime(row.ExpiresAtUtc));
                WriteTextCell(writer, column++, excelRow, FormatDateTime(row.PaidAtUtc));

                if (canViewDetails)
                {
                    WriteTextCell(writer, column++, excelRow, row.ResNum);
                    WriteTextCell(writer, column++, excelRow, row.RefNum);
                    WriteTextCell(writer, column++, excelRow, row.TraceNo);

                    if (row.VerifiedAmount.HasValue)
                        WriteNumberCell(writer, column++, excelRow, row.VerifiedAmount.Value, styleIndex: 2);
                    else
                        WriteTextCell(writer, column++, excelRow, null);

                    WriteTextCell(writer, column++, excelRow, row.SettlementIban);
                    WriteTextCell(writer, column++, excelRow, FormatDateTime(row.LastCallbackAtUtc));
                    WriteTextCell(writer, column++, excelRow, row.FailureMessage);
                }

                writer.WriteEndElement();
                excelRow++;
            }

            writer.WriteEndElement();

            writer.WriteStartElement("autoFilter", ns);
            writer.WriteAttributeString("ref", $"A1:{lastColumn}{lastRow}");
            writer.WriteEndElement();

            writer.WriteEndElement();
        });
    }

    private static void WriteContentTypes(ZipArchive archive)
    {
        WriteXmlEntry(archive, "[Content_Types].xml", writer =>
        {
            const string ns = "http://schemas.openxmlformats.org/package/2006/content-types";
            writer.WriteStartElement("Types", ns);

            WriteContentTypeDefault(writer, ns, "rels", "application/vnd.openxmlformats-package.relationships+xml");
            WriteContentTypeDefault(writer, ns, "xml", "application/xml");
            WriteContentTypeOverride(writer, ns, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
            WriteContentTypeOverride(writer, ns, "/xl/worksheets/sheet1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
            WriteContentTypeOverride(writer, ns, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");

            writer.WriteEndElement();
        });
    }

    private static void WriteRootRelationships(ZipArchive archive)
    {
        WriteXmlEntry(archive, "_rels/.rels", writer =>
        {
            const string ns = "http://schemas.openxmlformats.org/package/2006/relationships";
            writer.WriteStartElement("Relationships", ns);
            WriteRelationship(
                writer,
                ns,
                "rId1",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument",
                "xl/workbook.xml");
            writer.WriteEndElement();
        });
    }

    private static void WriteWorkbook(ZipArchive archive)
    {
        WriteXmlEntry(archive, "xl/workbook.xml", writer =>
        {
            const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            const string relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            writer.WriteStartElement("workbook", ns);
            writer.WriteAttributeString("xmlns", "r", null, relNs);

            writer.WriteStartElement("sheets", ns);
            writer.WriteStartElement("sheet", ns);
            writer.WriteAttributeString("name", "گزارش پرداخت‌ها");
            writer.WriteAttributeString("sheetId", "1");
            writer.WriteAttributeString("r", "id", relNs, "rId1");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
        });
    }

    private static void WriteWorkbookRelationships(ZipArchive archive)
    {
        WriteXmlEntry(archive, "xl/_rels/workbook.xml.rels", writer =>
        {
            const string ns = "http://schemas.openxmlformats.org/package/2006/relationships";
            writer.WriteStartElement("Relationships", ns);
            WriteRelationship(
                writer,
                ns,
                "rId1",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet",
                "worksheets/sheet1.xml");
            WriteRelationship(
                writer,
                ns,
                "rId2",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles",
                "styles.xml");
            writer.WriteEndElement();
        });
    }

    private static void WriteStyles(ZipArchive archive)
    {
        WriteXmlEntry(archive, "xl/styles.xml", writer =>
        {
            const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            writer.WriteStartElement("styleSheet", ns);

            writer.WriteStartElement("numFmts", ns);
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("numFmt", ns);
            writer.WriteAttributeString("numFmtId", "164");
            writer.WriteAttributeString("formatCode", "#,##0");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("fonts", ns);
            writer.WriteAttributeString("count", "2");
            WriteFont(writer, ns, bold: false, color: null);
            WriteFont(writer, ns, bold: true, color: "FFFFFFFF");
            writer.WriteEndElement();

            writer.WriteStartElement("fills", ns);
            writer.WriteAttributeString("count", "3");
            WritePatternFill(writer, ns, "none", null);
            WritePatternFill(writer, ns, "gray125", null);
            WritePatternFill(writer, ns, "solid", "FFE8192C");
            writer.WriteEndElement();

            writer.WriteStartElement("borders", ns);
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("border", ns);
            foreach (var edge in new[] { "left", "right", "top", "bottom", "diagonal" })
            {
                writer.WriteStartElement(edge, ns);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("cellStyleXfs", ns);
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("xf", ns);
            writer.WriteAttributeString("numFmtId", "0");
            writer.WriteAttributeString("fontId", "0");
            writer.WriteAttributeString("fillId", "0");
            writer.WriteAttributeString("borderId", "0");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("cellXfs", ns);
            writer.WriteAttributeString("count", "3");

            WriteXf(writer, ns, numFmtId: 0, fontId: 0, fillId: 0, applyAlignment: true, horizontal: "right");
            WriteXf(writer, ns, numFmtId: 0, fontId: 1, fillId: 2, applyAlignment: true, horizontal: "center");
            WriteXf(writer, ns, numFmtId: 164, fontId: 0, fillId: 0, applyAlignment: true, horizontal: "right", applyNumberFormat: true);

            writer.WriteEndElement();

            writer.WriteStartElement("cellStyles", ns);
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("cellStyle", ns);
            writer.WriteAttributeString("name", "Normal");
            writer.WriteAttributeString("xfId", "0");
            writer.WriteAttributeString("builtinId", "0");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
        });
    }

    private static void WriteXmlEntry(ZipArchive archive, string path, Action<XmlWriter> write)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            CloseOutput = false
        });

        writer.WriteStartDocument(true);
        write(writer);
        writer.WriteEndDocument();
    }

    private static void WriteTextCell(XmlWriter writer, int column, int row, string? value, int styleIndex = 0)
    {
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        writer.WriteStartElement("c", ns);
        writer.WriteAttributeString("r", $"{GetExcelColumnName(column)}{row}");
        writer.WriteAttributeString("t", "inlineStr");
        if (styleIndex > 0)
            writer.WriteAttributeString("s", styleIndex.ToString(CultureInfo.InvariantCulture));

        writer.WriteStartElement("is", ns);
        writer.WriteStartElement("t", ns);
        writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
        writer.WriteString(value ?? string.Empty);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteNumberCell(XmlWriter writer, int column, int row, long value, int styleIndex = 0)
    {
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        writer.WriteStartElement("c", ns);
        writer.WriteAttributeString("r", $"{GetExcelColumnName(column)}{row}");
        writer.WriteAttributeString("t", "n");
        if (styleIndex > 0)
            writer.WriteAttributeString("s", styleIndex.ToString(CultureInfo.InvariantCulture));

        writer.WriteStartElement("v", ns);
        writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static string GetExcelColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + (column % 26)) + name;
            column /= 26;
        }

        return name;
    }

    private static void WriteContentTypeDefault(XmlWriter writer, string ns, string extension, string contentType)
    {
        writer.WriteStartElement("Default", ns);
        writer.WriteAttributeString("Extension", extension);
        writer.WriteAttributeString("ContentType", contentType);
        writer.WriteEndElement();
    }

    private static void WriteContentTypeOverride(XmlWriter writer, string ns, string partName, string contentType)
    {
        writer.WriteStartElement("Override", ns);
        writer.WriteAttributeString("PartName", partName);
        writer.WriteAttributeString("ContentType", contentType);
        writer.WriteEndElement();
    }

    private static void WriteRelationship(XmlWriter writer, string ns, string id, string type, string target)
    {
        writer.WriteStartElement("Relationship", ns);
        writer.WriteAttributeString("Id", id);
        writer.WriteAttributeString("Type", type);
        writer.WriteAttributeString("Target", target);
        writer.WriteEndElement();
    }

    private static void WriteFont(XmlWriter writer, string ns, bool bold, string? color)
    {
        writer.WriteStartElement("font", ns);
        if (bold)
        {
            writer.WriteStartElement("b", ns);
            writer.WriteEndElement();
        }

        if (!string.IsNullOrWhiteSpace(color))
        {
            writer.WriteStartElement("color", ns);
            writer.WriteAttributeString("rgb", color);
            writer.WriteEndElement();
        }

        writer.WriteStartElement("sz", ns);
        writer.WriteAttributeString("val", "11");
        writer.WriteEndElement();

        writer.WriteStartElement("name", ns);
        writer.WriteAttributeString("val", "Tahoma");
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private static void WritePatternFill(XmlWriter writer, string ns, string patternType, string? color)
    {
        writer.WriteStartElement("fill", ns);
        writer.WriteStartElement("patternFill", ns);
        writer.WriteAttributeString("patternType", patternType);

        if (!string.IsNullOrWhiteSpace(color))
        {
            writer.WriteStartElement("fgColor", ns);
            writer.WriteAttributeString("rgb", color);
            writer.WriteEndElement();
            writer.WriteStartElement("bgColor", ns);
            writer.WriteAttributeString("indexed", "64");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteXf(
        XmlWriter writer,
        string ns,
        int numFmtId,
        int fontId,
        int fillId,
        bool applyAlignment,
        string horizontal,
        bool applyNumberFormat = false)
    {
        writer.WriteStartElement("xf", ns);
        writer.WriteAttributeString("numFmtId", numFmtId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fontId", fontId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fillId", fillId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("borderId", "0");
        writer.WriteAttributeString("xfId", "0");

        if (applyNumberFormat)
            writer.WriteAttributeString("applyNumberFormat", "1");
        if (applyAlignment)
            writer.WriteAttributeString("applyAlignment", "1");

        writer.WriteStartElement("alignment", ns);
        writer.WriteAttributeString("horizontal", horizontal);
        writer.WriteAttributeString("vertical", "center");
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private IQueryable<BranchPaymentLink> BuildScopeQuery(DateTime fromUtc, DateTime toUtcExclusive)
    {
        var query = db.BranchPaymentLinks
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toUtcExclusive);

        if (Filter.BranchId.HasValue)
            query = query.Where(x => x.BranchId == Filter.BranchId.Value);

        if (Filter.Opened.HasValue)
            query = Filter.Opened.Value
                ? query.Where(x => x.OpenedAtUtc.HasValue)
                : query.Where(x => !x.OpenedAtUtc.HasValue);

        var search = Filter.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var numericId = long.TryParse(search, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId)
                ? parsedId
                : (long?)null;

            query = query.Where(x =>
                (numericId.HasValue && x.Id == numericId.Value) ||
                x.PublicToken.Contains(search) ||
                x.ResNum.Contains(search) ||
                x.SettlementIban.Contains(search) ||
                (x.RefNum != null && x.RefNum.Contains(search)) ||
                (x.TraceNo != null && x.TraceNo.Contains(search)) ||
                (x.CellNumber != null && x.CellNumber.Contains(search)) ||
                (x.Branch != null && x.Branch.Name.Contains(search)));
        }

        return query;
    }

    private IQueryable<BranchPaymentLink> ApplyStatusFilter(IQueryable<BranchPaymentLink> query, DateTime nowUtc)
    {
        if (!Filter.Status.HasValue)
            return query;

        var status = Filter.Status.Value;
        if (status == PaymentLinkStatus.Expired)
        {
            return query.Where(x =>
                x.Status == PaymentLinkStatus.Expired ||
                (x.Status != PaymentLinkStatus.Paid &&
                 x.Status != PaymentLinkStatus.Failed &&
                 x.ExpiresAtUtc.HasValue &&
                 x.ExpiresAtUtc.Value <= nowUtc));
        }

        return query.Where(x =>
            x.Status == status &&
            !(x.Status != PaymentLinkStatus.Paid &&
              x.Status != PaymentLinkStatus.Failed &&
              x.ExpiresAtUtc.HasValue &&
              x.ExpiresAtUtc.Value <= nowUtc));
    }

    private void NormalizeFilterDates()
    {
        var today = FormatPersianDate(IranClock.Now);
        Filter.DateFrom = string.IsNullOrWhiteSpace(Filter.DateFrom) ? today : NormalizeDigits(Filter.DateFrom.Trim());
        Filter.DateTo = string.IsNullOrWhiteSpace(Filter.DateTo) ? today : NormalizeDigits(Filter.DateTo.Trim());
        Filter.Search = string.IsNullOrWhiteSpace(Filter.Search) ? null : Filter.Search.Trim();
        Filter.PageNumber = Math.Max(1, Filter.PageNumber);
    }

    private bool TryGetUtcRange(out DateTime fromUtc, out DateTime toUtcExclusive)
    {
        fromUtc = default;
        toUtcExclusive = default;

        if (!TryParsePersianDate(Filter.DateFrom, out var fromLocal))
        {
            ModelState.AddModelError("Filter.DateFrom", "تاریخ شروع معتبر نیست.");
            return false;
        }

        if (!TryParsePersianDate(Filter.DateTo, out var toLocal))
        {
            ModelState.AddModelError("Filter.DateTo", "تاریخ پایان معتبر نیست.");
            return false;
        }

        if (toLocal < fromLocal)
        {
            ModelState.AddModelError("Filter.DateTo", "تاریخ پایان نمی‌تواند قبل از تاریخ شروع باشد.");
            return false;
        }

        fromUtc = IranClock.ToUtc(fromLocal.Date);
        toUtcExclusive = IranClock.ToUtc(toLocal.Date.AddDays(1));
        return true;
    }

    private async Task LoadBranchesAsync()
    {
        Branches = await db.Branches.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new BranchOption(x.Id, x.Name))
            .ToListAsync();
    }

    public PaymentLinkStatus GetEffectiveStatus(ReportRow row)
    {
        if (row.Status is PaymentLinkStatus.Paid or PaymentLinkStatus.Failed or PaymentLinkStatus.Expired)
            return row.Status;

        return row.ExpiresAtUtc.HasValue && row.ExpiresAtUtc.Value <= DateTime.UtcNow
            ? PaymentLinkStatus.Expired
            : row.Status;
    }

    public static string GetStatusTitle(PaymentLinkStatus status) => status switch
    {
        PaymentLinkStatus.Created => "باز نشده",
        PaymentLinkStatus.Opened => "در حال اتصال به درگاه",
        PaymentLinkStatus.GatewayReady => "در انتظار پرداخت",
        PaymentLinkStatus.VerificationPending => "در انتظار تایید بانک",
        PaymentLinkStatus.Paid => "پرداخت موفق",
        PaymentLinkStatus.Failed => "ناموفق",
        PaymentLinkStatus.Expired => "منقضی",
        _ => "نامشخص"
    };

    public static string GetStatusCss(PaymentLinkStatus status) => status switch
    {
        PaymentLinkStatus.Paid => "on",
        PaymentLinkStatus.Failed => "off",
        PaymentLinkStatus.Expired => "off",
        PaymentLinkStatus.VerificationPending => "pending",
        _ => "pending"
    };

    public string FormatDateTime(DateTime? utc)
    {
        if (!utc.HasValue) return "-";
        var value = IranClock.FromUtc(utc.Value);
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00} {value:HH:mm:ss}";
    }

    public string MaskIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return "-";
        return iban.Length <= 8 ? iban : $"{iban[..4]}••••••••••••••••{iban[^4..]}";
    }

    public string ShortPaymentKey(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return "-";
        return token.Length <= 12 ? token : $"{token[..6]}…{token[^6..]}";
    }

    public string BuildPageUrl(int page)
    {
        var pairs = BuildQueryPairs();
        pairs.Add(new("Filter.PageNumber", Math.Max(1, page).ToString(CultureInfo.InvariantCulture)));
        return "?" + string.Join("&", pairs.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));
    }

    public string BuildExportUrl()
    {
        var pairs = BuildQueryPairs();
        pairs.Add(new("handler", "Export"));
        return "?" + string.Join("&", pairs.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));
    }

    private List<KeyValuePair<string, string?>> BuildQueryPairs()
    {
        var pairs = new List<KeyValuePair<string, string?>>
        {
            new("Filter.DateFrom", Filter.DateFrom),
            new("Filter.DateTo", Filter.DateTo),
            new("Filter.PageSize", Filter.PageSize.ToString(CultureInfo.InvariantCulture))
        };

        if (Filter.BranchId.HasValue)
            pairs.Add(new("Filter.BranchId", Filter.BranchId.Value.ToString(CultureInfo.InvariantCulture)));
        if (Filter.Status.HasValue)
            pairs.Add(new("Filter.Status", ((int)Filter.Status.Value).ToString(CultureInfo.InvariantCulture)));
        if (Filter.Opened.HasValue)
            pairs.Add(new("Filter.Opened", Filter.Opened.Value ? "true" : "false"));
        if (!string.IsNullOrWhiteSpace(Filter.Search))
            pairs.Add(new("Filter.Search", Filter.Search));

        return pairs;
    }

    private static bool TryParsePersianDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var parts = NormalizeDigits(value).Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month) ||
            !int.TryParse(parts[2], out var day))
            return false;

        try
        {
            date = new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static string FormatPersianDate(DateTime value)
    {
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00}";
    }

    private static string NormalizeDigits(string value)
    {
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        const string arabic = "٠١٢٣٤٥٦٧٨٩";
        for (var i = 0; i < 10; i++)
            value = value.Replace(persian[i], (char)('0' + i)).Replace(arabic[i], (char)('0' + i));
        return value;
    }

}
