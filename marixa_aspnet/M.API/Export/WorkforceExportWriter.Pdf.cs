using System.Globalization;
using M.ModelViews.AttendancePeriodModelView;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace M.API.Export;

public sealed partial class WorkforceExportWriter
{
    private static readonly object FontGate = new();

    public byte[] LeavePdf(LeavePdfData data)
    {
        lock (FontGate) GlobalFontSettings.FontResolver ??= new NotoFontResolver();
        using var document = new PdfDocument();
        document.Info.Title = "Đơn nghỉ phép - Marixa";
        document.Info.Creator = "Marixa";
        var font = new XFont("MarixaNoto", 11, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        var heading = new XFont("MarixaNoto", 20, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        PdfPage page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        XGraphics graphics = XGraphics.FromPdfPage(page);
        var y = 48d;
        try
        {
            graphics.DrawString("MARIXA", heading, XBrushes.DarkSlateBlue, new XPoint(44, y));
            y += 38;
            Line("ĐƠN NGHỈ PHÉP", heading);
            y += 18;
            Wrap($"Mã đơn: {data.Id}");
            Wrap($"Nhân viên: {data.EmployeeName} ({data.EmployeeCode})");
            Wrap($"Loại nghỉ: {data.LeaveType}");
            Wrap($"Thời gian: {data.FromDate:dd/MM/yyyy} - {data.ToDate:dd/MM/yyyy}");
            Wrap($"Số ngày: {data.TotalDays?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Chưa xác định"}");
            Wrap($"Trạng thái: {data.Status switch { M.Contract.Repositories.Entities.LeaveRequestStatus.Approved => "Đã duyệt", M.Contract.Repositories.Entities.LeaveRequestStatus.Rejected => "Từ chối", M.Contract.Repositories.Entities.LeaveRequestStatus.Cancelled => "Đã hủy", _ => "Chờ duyệt" }}");
            y += 14;
            Line("Lý do:");
            Wrap(data.Reason ?? "Không có");
            y += 14;
            Wrap($"Người xử lý: {data.Approver ?? "Chưa xử lý"}");
            Wrap($"Xử lý lúc VN: {Vn(data.ApprovedAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(data.ApprovedAt.Value, DateTimeKind.Utc)) : null) ?? "Chưa xử lý"}");
            if (data.CancelledAt.HasValue)
            {
                Wrap($"Hủy lúc VN: {Vn(new DateTimeOffset(DateTime.SpecifyKind(data.CancelledAt.Value, DateTimeKind.Utc)))}");
                Wrap($"Lý do hủy: {data.CancellationReason ?? "Không có"}");
            }
            y += 18;
            Wrap($"Phiên bản dữ liệu UTC: {data.DataVersion:O}");
            Wrap($"Xuất lúc VN: {Vn(data.ExportedAt)}");
            Wrap("Bản xuất từ dữ liệu hệ thống tại thời điểm ghi trên tài liệu.");
            graphics.Dispose();
            for (var index = 0; index < document.Pages.Count; index++)
            {
                using var footer = XGraphics.FromPdfPage(document.Pages[index], XGraphicsPdfPageOptions.Append);
                footer.DrawString($"Marixa | {index + 1}/{document.Pages.Count}", font, XBrushes.Gray,
                    new XPoint(44, document.Pages[index].Height.Point - 30));
            }
            using var stream = new MemoryStream();
            document.Save(stream, closeStream: false);
            return stream.ToArray();
        }
        finally { graphics.Dispose(); }

        void NewPageIfNeeded()
        {
            if (y < page.Height.Point - 65) return;
            graphics.Dispose();
            page = document.AddPage(); page.Size = PdfSharp.PageSize.A4;
            graphics = XGraphics.FromPdfPage(page); y = 48;
        }
        void Line(string value, XFont? selected = null)
        {
            NewPageIfNeeded();
            graphics.DrawString(value, selected ?? font, XBrushes.Black, new XPoint(44, y));
            y += selected == heading ? 28 : 20;
        }
        void Wrap(string value)
        {
            foreach (var paragraph in value.Replace("\r", "").Split('\n'))
            {
                var current = "";
                foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var next = current.Length == 0 ? word : current + " " + word;
                    if (graphics.MeasureString(next, font).Width <= page.Width.Point - 88)
                    { current = next; continue; }
                    if (current.Length > 0) Line(current);
                    current = "";
                    foreach (var rune in word.EnumerateRunes())
                    {
                        next = current + rune;
                        if (graphics.MeasureString(next, font).Width > page.Width.Point - 88 && current.Length > 0)
                        { Line(current); current = rune.ToString(); }
                        else current = next;
                    }
                }
                Line(current);
            }
        }
    }

    private sealed class NotoFontResolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
            familyName == "MarixaNoto" ? new FontResolverInfo("NotoSans-Regular") : null;
        public byte[]? GetFont(string faceName) => faceName == "NotoSans-Regular"
            ? File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "NotoSans-Regular.ttf")) : null;
    }
}
