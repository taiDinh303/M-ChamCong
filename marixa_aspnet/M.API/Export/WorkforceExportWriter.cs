using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using M.Contract.Services.Export;
using M.ModelViews.AttendancePeriodModelView;

namespace M.API.Export;

public sealed partial class WorkforceExportWriter : IWorkforceExportWriter
{
    public byte[] AttendanceXlsx(PeriodSnapshot snapshot, string sha256)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Text(zip, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
            Text(zip, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Text(zip, "xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Bảng công\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"Thông tin chốt\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>");
            Text(zip, "xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            Text(zip, "xl/styles.xml", "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF4F46E5\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"3\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"2\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");
            var rows = new List<object?[]>
            {
                new object?[] { "Mã nhân viên", "Họ tên", "Ngày", "Trạng thái", "Duyệt", "Giờ vào VN", "Giờ ra VN", "Giờ thực", "Giờ OT duyệt", "Mã bản ghi" }
            };
            rows.AddRange(snapshot.Rows.Select(row => new object?[]
            {
                row.EmployeeCode, row.EmployeeName, row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                StatusLabel(row.Status), "Đã duyệt", Vn(row.CheckIn), Vn(row.CheckOut),
                row.ActualHours, row.OvertimeHours, row.AttendanceId.ToString()
            }));
            Sheet(zip, "xl/worksheets/sheet1.xml", rows, freeze: true);
            Sheet(zip, "xl/worksheets/sheet2.xml", new List<object?[]>
            {
                new object?[] { "Thuộc tính", "Giá trị" }, new object?[] { "Kỳ", $"{snapshot.Year:D4}-{snapshot.Month:D2}" },
                new object?[] { "Phiên bản snapshot", snapshot.Version }, new object?[] { "Chốt lúc UTC", snapshot.CapturedAt.ToString("O") },
                new object?[] { "SHA-256 của dữ liệu trả về", sha256 }, new object?[] { "Mã ca", snapshot.Rule.Code },
                new object?[] { "Giờ chuẩn", $"{snapshot.Rule.CheckIn:HH:mm} - {snapshot.Rule.CheckOut:HH:mm}" },
                new object?[] { "Nghỉ bắt đầu", snapshot.Rule.BreakStartTime.ToString("HH:mm") },
                new object?[] { "Nghỉ (phút)", snapshot.Rule.BreakMinutes },
                new object?[] { "Cách đọc", "Dữ liệu tại lần khóa; ô trống là chưa đủ mốc. Giờ thường/OT tách riêng theo đơn duyệt và rule lưu tại lần khóa." }
            });
        }
        return stream.ToArray();
    }

    private static void Sheet(ZipArchive zip, string name, IEnumerable<object?[]> rows, bool freeze = false)
    {
        using var output = zip.CreateEntry(name, CompressionLevel.Fastest).Open();
        using var xml = XmlWriter.Create(output, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        xml.WriteStartDocument();
        xml.WriteStartElement("worksheet", ns);
        if (freeze)
        {
            xml.WriteStartElement("sheetViews"); xml.WriteStartElement("sheetView"); xml.WriteAttributeString("workbookViewId", "0");
            xml.WriteStartElement("pane"); xml.WriteAttributeString("ySplit", "1"); xml.WriteAttributeString("topLeftCell", "A2");
            xml.WriteAttributeString("activePane", "bottomLeft"); xml.WriteAttributeString("state", "frozen");
            xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndElement();
        }
        xml.WriteStartElement("cols");
        var widths = freeze ? new[] { 19, 32, 14, 18, 14, 23, 23, 14, 14, 40 } : new[] { 28, 100 };
        for (var index = 0; index < widths.Length; index++)
        {
            xml.WriteStartElement("col"); xml.WriteAttributeString("min", (index + 1).ToString());
            xml.WriteAttributeString("max", (index + 1).ToString()); xml.WriteAttributeString("width", widths[index].ToString());
            xml.WriteAttributeString("customWidth", "1"); xml.WriteEndElement();
        }
        xml.WriteEndElement();
        xml.WriteStartElement("sheetData");
        var rowNumber = 0;
        foreach (var row in rows)
        {
            xml.WriteStartElement("row"); xml.WriteAttributeString("r", (++rowNumber).ToString(CultureInfo.InvariantCulture));
            if (rowNumber == 1) { xml.WriteAttributeString("ht", "30"); xml.WriteAttributeString("customHeight", "1"); }
            for (var column = 0; column < row.Length; column++)
            {
                xml.WriteStartElement("c"); xml.WriteAttributeString("r", $"{(char)('A' + column)}{rowNumber}");
                if (rowNumber == 1) xml.WriteAttributeString("s", "1");
                else if (row[column] is decimal) xml.WriteAttributeString("s", "2");
                if (row[column] is int or decimal)
                    xml.WriteElementString("v", Convert.ToString(row[column], CultureInfo.InvariantCulture));
                else
                {
                    xml.WriteAttributeString("t", "inlineStr"); xml.WriteStartElement("is");
                    xml.WriteStartElement("t"); xml.WriteAttributeString("xml", "space", null, "preserve");
                    // Text cells never contain formula elements, including values beginning with '='.
                    xml.WriteString(Clean(Convert.ToString(row[column], CultureInfo.InvariantCulture) ?? ""));
                    xml.WriteEndElement(); xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
        if (freeze && rowNumber > 1) { xml.WriteStartElement("autoFilter"); xml.WriteAttributeString("ref", $"A1:J{rowNumber}"); xml.WriteEndElement(); }
        xml.WriteEndElement(); xml.WriteEndDocument();
    }

    private static string? StatusLabel(M.Contract.Repositories.Entities.AttendanceStatus? status) => status switch
    {
        M.Contract.Repositories.Entities.AttendanceStatus.Present => "Đúng giờ",
        M.Contract.Repositories.Entities.AttendanceStatus.Late => "Đi trễ",
        M.Contract.Repositories.Entities.AttendanceStatus.EarlyLeave => "Về sớm",
        M.Contract.Repositories.Entities.AttendanceStatus.Absent => "Vắng mặt",
        M.Contract.Repositories.Entities.AttendanceStatus.Leave => "Nghỉ phép",
        M.Contract.Repositories.Entities.AttendanceStatus.Holiday => "Nghỉ lễ",
        M.Contract.Repositories.Entities.AttendanceStatus.Weekend => "Cuối tuần",
        _ => null
    };

    private static string Clean(string value) => new(value.Where(c => XmlConvert.IsXmlChar(c)).Take(32767).ToArray());
    private static string? Vn(DateTimeOffset? time) => time?.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    private static void Text(ZipArchive zip, string name, string contents)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(contents);
    }
}
