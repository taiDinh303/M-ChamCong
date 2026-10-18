using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using M.API.Configuration;
using M.API.Seed;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using M.Repositories.Configuration;
using M.Repositories.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

// Separate operator tool: the deployed API never invokes this seeder automatically.
var apply = args.Contains("--apply");
var verifyOnly = args.Contains("--verify-only");
if (apply && verifyOnly) throw new InvalidOperationException("Choose apply or verify-only, not both.");
var local = args.Contains("--local");
var expected = args.Contains("--supabase-marixa001");
if (local == expected) throw new InvalidOperationException("Choose exactly --local or --supabase-marixa001.");
var apiRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../M.API"));
var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(apiRoot, "appsettings.json"))
    .AddUserSecrets(typeof(SampleDataSeeder).Assembly, optional: true).AddEnvironmentVariables("MARIXA_DEMO_").Build();
var connection = config["ConnectionString"] ?? config.GetConnectionString("MyCnn") ?? throw new InvalidOperationException("Database configuration is missing.");
var services = new ServiceCollection();
services.AddLogging();
if (local)
{
    var sql = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
    if (!sql.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) || !sql.InitialCatalog.StartsWith("Marixa_Demo_", StringComparison.Ordinal))
        throw new InvalidOperationException("Local mode requires a dedicated Marixa_Demo_* LocalDB database.");
    services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(connection));
}
else
{
    connection = PostgresConnectionString.Normalize(connection);
    var pg = new NpgsqlConnectionStringBuilder(connection);
    if (!(pg.Host?.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) == true || pg.Host?.EndsWith(".supabase.com", StringComparison.OrdinalIgnoreCase) == true)
        || !(pg.Host.Contains("zphqfcwrwitrqdeueonu", StringComparison.Ordinal) || pg.Username?.EndsWith(".zphqfcwrwitrqdeueonu", StringComparison.Ordinal) == true))
        throw new InvalidOperationException("Supabase target does not match the explicitly authorized marixa001 project.");
    if (pg.SslMode is SslMode.Disable or SslMode.Allow or SslMode.Prefer) throw new InvalidOperationException("TLS is required.");
    services.AddDbContext<DatabaseContext>(options => options.UseNpgsql(connection));
}
DependencyInjection.AddIdentity(services);
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
if (!apply && !verifyOnly)
{
    Console.WriteLine(JsonSerializer.Serialize(new { Mode = "PLAN_ONLY", Target = local ? "isolated-localdb" : "marixa001", Batch = DemoScenarioSeeder.Marker,
        NewEmployees = 6, AccountRole = "Employee", ExistingRecords = "preserved", HistoricPeriod = "2026-09 (only if empty)", CurrentPeriod = "2026-10 stays open", SecurityTables = "no fabricated claims, tokens, logins, activation codes or migration history" }));
    return;
}
var password = config["Password"] ?? throw new InvalidOperationException("Supply a private DEMO password through MARIXA_DEMO_Password; never pass it as an argument.");
if (password.Length < 16) throw new InvalidOperationException("Use a private DEMO password of at least 16 characters.");
if (local && !verifyOnly)
{
    await db.Database.EnsureCreatedAsync();
    await RoleSeeder.SeedAsync(scope.ServiceProvider);
    await AttendanceRuleSeeder.SeedAsync(scope.ServiceProvider);
    var localUsers = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (!await db.Users.AnyAsync(u => u.UserName == "demo.local-admin"))
    {
        var employee = new Employee { EmployeeCode = "LOCAL-REVIEWER", GivenName = "Local", FamilyName = "Reviewer", Status = EmployeeStatus.Working };
        db.Employees.Add(employee); await db.SaveChangesAsync();
        var admin = new ApplicationUser { UserName = "demo.local-admin", Email = "local-admin@example.invalid", EmployeeId = employee.Id, CreatedBy = DemoScenarioSeeder.Marker };
        if (!(await localUsers.CreateAsync(admin, password)).Succeeded || !(await localUsers.AddToRoleAsync(admin, "Admin")).Succeeded)
            throw new InvalidOperationException("Could not provision the isolated local reviewer.");
        employee.UserId = admin.Id; await db.SaveChangesAsync();
    }
}
var before = local || verifyOnly ? null : await OriginalRows.ReadAsync(connection, db);
var seeded = !verifyOnly && await DemoScenarioSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), password);
if (before != null)
{
    var after = await OriginalRows.ReadAsync(connection, db);
    if (before.Any(table => !table.Value.IsSubsetOf(after[table.Key])))
        throw new InvalidOperationException("Original row fingerprint changed; inspect immediately.");
    Console.WriteLine(JsonSerializer.Serialize(new { OriginalTablesPreserved = before.Count, OriginalRowFingerprintsPreserved = before.Sum(t => t.Value.Count) }));
}
Console.WriteLine(JsonSerializer.Serialize(new { Seeded = seeded, Batch = DemoScenarioSeeder.Marker,
    DemoEmployees = await db.Employees.CountAsync(e => e.CreatedBy == DemoScenarioSeeder.Marker), DemoAttendance = await db.Attendances.CountAsync(a => a.CreatedBy == DemoScenarioSeeder.Marker) }));

if (!verifyOnly) await DemoScenarioSeeder.PrepareReportDraftsAsync(db);
var api = config["ApiUrl"];
if (string.IsNullOrWhiteSpace(api)) return;
var apiUri = new Uri(api);
if (local ? !apiUri.IsLoopback : apiUri.AbsoluteUri != "https://marixa001-asp.onrender.com/") throw new InvalidOperationException("Unexpected report-upload API destination.");
using var http = new HttpClient { BaseAddress = apiUri, Timeout = TimeSpan.FromSeconds(90) };
var reportsUploaded = 0;
for (var i = 0; i < 3; i++)
{
    var employeeId = DemoScenarioSeeder.Id($"employee-{i}");
    var title = $"[DEMO] Báo cáo vận hành tháng 10 — {DemoScenarioSeeder.AccountNames[i]}";
    var report = await db.EmployeeReports.SingleOrDefaultAsync(r => r.EmployeeId == employeeId && r.Title == title);
    using var login = await http.PostAsJsonAsync("api/Auth/login", new { username = DemoScenarioSeeder.AccountNames[i], password, rememberMe = false });
    if (!login.IsSuccessStatusCode) throw new InvalidOperationException($"DEMO report login failed ({(int)login.StatusCode}).");
    using var auth = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
    var token = auth.RootElement.GetProperty("data").GetProperty("token").GetString();
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (report != null && await db.EmployeeReportVersions.AnyAsync(v => v.ReportId == report.Id))
    {
        var versionId = await db.EmployeeReportVersions.Where(v => v.ReportId == report.Id).Select(v => v.Id).FirstAsync();
        using var download = await http.GetAsync($"api/EmployeeReport/download/{versionId}");
        var bytes = await download.Content.ReadAsByteArrayAsync();
        if (!download.IsSuccessStatusCode || !bytes.AsSpan().StartsWith("%PDF-"u8)) throw new InvalidOperationException("DEMO private report download failed.");
        using var attendance = await http.GetAsync($"api/Attendance/by-employee/{employeeId}");
        using var leave = await http.GetAsync("api/LeaveRequest/mine");
        if (!attendance.IsSuccessStatusCode || !leave.IsSuccessStatusCode) throw new InvalidOperationException("DEMO owner API smoke failed.");
        Console.WriteLine(JsonSerializer.Serialize(new { DemoAccount = DemoScenarioSeeder.AccountNames[i], PrivatePdf = (int)download.StatusCode,
            Attendance = (int)attendance.StatusCode, Leave = (int)leave.StatusCode }));
        continue;
    }
    if (verifyOnly) throw new InvalidOperationException("DEMO report files are not ready to verify.");
    using var form = new MultipartFormDataContent();
    if (report != null) form.Add(new StringContent(report.Id.ToString()), "ReportId");
    form.Add(new StringContent(title), "Title"); form.Add(new StringContent("Vận hành [DEMO]"), "ReportType");
    form.Add(new StringContent("2026-10"), "Period"); form.Add(new StringContent("2026-10-25"), "Deadline");
    form.Add(new StringContent("[DEMO] Đối soát công, kiểm tra hồ sơ và cập nhật quy trình."), "Overview");
    form.Add(new StringContent("Hoàn thành 18/20 hồ sơ; còn hai hồ sơ cần bổ sung."), "Results");
    form.Add(new StringContent("Thiếu chứng từ đối soát ở hai trường hợp giả lập."), "Issues");
    form.Add(new StringContent("Bổ sung hồ sơ và xác nhận với trưởng nhóm trước hạn."), "Recommendations");
    var pdf = DemoReportPdf.Create();
    foreach (var name in new[] { "bao-cao-demo.pdf", "phu-luc-demo.pdf" })
    {
        var file = new ByteArrayContent(pdf); file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); form.Add(file, "Files", name);
    }
    using var result = await http.PostAsync("api/EmployeeReport/submit", form);
    if (!result.IsSuccessStatusCode) throw new InvalidOperationException($"DEMO report upload failed ({(int)result.StatusCode}); business seed is committed and upload may be safely resumed.");
    reportsUploaded++;
}
Console.WriteLine(JsonSerializer.Serialize(new { ReportsUploaded = reportsUploaded, Files = "Uploaded through the actual private-document API; no fake storage paths." }));

static class DemoReportPdf
{
    public static byte[] Create()
    {
        // A small real PDF with valid offsets; contains only synthetic English text.
        var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        const string content = "BT /F1 18 Tf 50 780 Td (MARIXA DEMO - Synthetic test report) Tj 0 -30 Td /F1 12 Tf (No real employee data. October 2026 operational review.) Tj ET";
        var text = new System.Text.StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 };
        foreach (var obj in objects.Append($"<< /Length {content.Length} >>\nstream\n{content}\nendstream"))
        { offsets.Add(text.Length); text.Append($"{offsets.Count - 1} 0 obj\n{obj}\nendobj\n"); }
        var xref = text.Length; text.Append($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) text.Append($"{offset:D10} 00000 n \n");
        text.Append($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return System.Text.Encoding.ASCII.GetBytes(text.ToString());
    }
}

static class OriginalRows
{
    public static async Task<Dictionary<string, HashSet<string>>> ReadAsync(string connection, DatabaseContext db)
    {
        await using var sql = new NpgsqlConnection(connection); await sql.OpenAsync();
        var result = new Dictionary<string, HashSet<string>>();
        foreach (var table in db.Model.GetEntityTypes().Select(e => e.GetTableName()).Where(n => n != null).Distinct())
        {
            var quoted = "\"" + table!.Replace("\"", "\"\"") + "\"";
            await using var command = new NpgsqlCommand($"SELECT encode(sha256(convert_to(to_jsonb(t)::text,'UTF8')),'hex') FROM public.{quoted} t", sql);
            await using var rows = await command.ExecuteReaderAsync(); var hashes = new HashSet<string>();
            while (await rows.ReadAsync()) hashes.Add(rows.GetString(0));
            result.Add(table, hashes);
        }
        return result;
    }
}
