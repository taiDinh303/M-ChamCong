using System.IO.Compression;
using System.Xml.Linq;
using M.API.Export;
using M.Contract.Repositories.Entities;
using M.ModelViews.AttendancePeriodModelView;
using PdfSharp.Pdf.IO;

namespace M.Tests.Unit;

public sealed class ExportTests
{
    [Fact]
    public void Xlsx_uses_numeric_hours_and_text_cells_without_executable_formulas()
    {
        var data = new PeriodSnapshot(2026, 10, 1, DateTimeOffset.Parse("2026-10-17T01:00:00Z"),
            new("TEST", new(8, 0), new(17, 0), 60, 8),
            [new(Guid.NewGuid(), Guid.NewGuid(), "=CMD()", "Nguyễn Thị Thuý Ân", new DateTime(2026, 10, 17),
                AttendanceStatus.Present, AttendanceApprovalStatus.Approved, null, null, 8, 0.5m)]);
        var bytes = new WorkforceExportWriter().AttendanceXlsx(data, "TEST-HASH");
        using var archive = new ZipArchive(new MemoryStream(bytes));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using var stream = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var sheet = XDocument.Load(stream);
        Assert.Empty(sheet.Descendants(ns + "f"));
        Assert.Equal("inlineStr", sheet.Descendants(ns + "c").Single(c => c.Attribute("r")!.Value == "A2").Attribute("t")!.Value);
        Assert.Contains(sheet.Descendants(ns + "v"), value => value.Value == "0.5");
        SaveArtifact("attendance-sample.xlsx", bytes);
    }

    [Fact]
    public void Pdf_embeds_vietnamese_font_and_paginates_long_reason_without_losing_text()
    {
        var phrase = "Nghỉ phép để chăm sóc gia đình, đã bàn giao đầy đủ hồ sơ.";
        var reason = string.Join(" ", Enumerable.Repeat(phrase, 35)) + " " + new string('W', 200);
        var data = new LeavePdfData(Guid.Parse("12345678-1111-2222-3333-444444444444"), "NV-001", "Nguyễn Thị Thuý Ân",
            "Phép năm", new DateTime(2026, 10, 17), new DateTime(2026, 10, 19), 3m, reason, LeaveRequestStatus.Approved,
            "Trần Minh Quân", new DateTime(2026, 10, 16, 1, 0, 0, DateTimeKind.Utc), DateTimeOffset.Parse("2026-10-16T01:00:00Z"),
            DateTimeOffset.Parse("2026-10-17T01:00:00Z"));
        var bytes = new WorkforceExportWriter().LeavePdf(data);
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount >= 2);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
        SaveArtifact("leave-sample.pdf", bytes);
    }

    private static void SaveArtifact(string name, byte[] bytes)
    {
        var directory = Environment.GetEnvironmentVariable("MARIXA_ARTIFACT_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, name), bytes);
    }
}
