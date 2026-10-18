using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using M.Contract.Repositories.Entities;
using M.ModelViews.AttendancePeriodModelView;
using M.Repositories.Context;
using M.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class PeriodTests
{
    [Theory]
    [InlineData("upper", true)]
    [InlineData("lower", true)]
    [InlineData("changed-payload", false)]
    [InlineData("invalid-hex", false)]
    public async Task Snapshot_hash_accepts_equivalent_hex_case_but_rejects_tampering(string format, bool valid)
    {
        using var app = Prepare();
        string storedJson;
        string storedHash;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var period = AttendancePeriod.Open(2026, 10);
            period.Review(ApiFactory.UserA, app.Clock.GetUtcNow(), "Fixture reviewed");
            var version = period.Lock(app.Clock.GetUtcNow());
            var rows = await db.Attendances.Include(a => a.Employee).ToListAsync();
            var payload = new PeriodSnapshot(2026, 10, version, app.Clock.GetUtcNow(),
                new SnapshotRule("TEST", new(8, 0), new(17, 0), 60, 8),
                rows.Select(a => new SnapshotRow(a.Id, a.EmployeeId, a.Employee!.EmployeeCode, a.Employee.FullName,
                    a.AttendanceDate, a.Status, a.ApprovalStatus, a.CheckInTime, a.CheckOutTime, 8m, 0m)).ToArray());
            storedJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            storedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(storedJson)));
            if (format == "lower") storedHash = storedHash.ToLowerInvariant();
            if (format == "invalid-hex") storedHash = new string('Z', 64);
            if (format == "changed-payload") storedJson = storedJson.Replace("TEST-A", "ALTERED");
            db.AttendancePeriods.Add(period);
            db.AttendancePeriodSnapshots.Add(AttendancePeriodSnapshot.Capture(period.Id, version, storedJson,
                storedHash, "fixture", app.Clock.GetUtcNow()));
            await db.SaveChangesAsync();
        }
        using var employee = app.Actor();
        var snapshot = await employee.GetAsync("/api/AttendancePeriod/2026/10/snapshot?version=1");
        var export = await employee.GetAsync("/api/AttendancePeriod/2026/10/export.xlsx?version=1");
        Assert.Equal(valid ? HttpStatusCode.OK : HttpStatusCode.Conflict, snapshot.StatusCode);
        Assert.Equal(valid ? HttpStatusCode.OK : HttpStatusCode.Conflict, export.StatusCode);
        if (valid)
        {
            var result = JsonDocument.Parse(await snapshot.Content.ReadAsStringAsync());
            var row = Assert.Single(result.RootElement.GetProperty("data").GetProperty("data").GetProperty("rows").EnumerateArray());
            Assert.Equal(ApiFactory.EmployeeA, row.GetProperty("employeeId").GetGuid());
            using var zip = new ZipArchive(new MemoryStream(await export.Content.ReadAsByteArrayAsync()));
            using var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
            var xml = await reader.ReadToEndAsync();
            Assert.Contains("TEST-A", xml);
            Assert.DoesNotContain("TEST-B", xml);
        }
        else Assert.Contains("SNAPSHOT_INTEGRITY", await snapshot.Content.ReadAsStringAsync());
        using var check = app.Services.CreateScope();
        var stored = await check.ServiceProvider.GetRequiredService<DatabaseContext>().AttendancePeriodSnapshots.AsNoTracking().SingleAsync();
        Assert.Equal(storedJson, stored.PayloadJson);
        Assert.Equal(storedHash, stored.Sha256);
    }

    [Fact]
    public async Task Period_requires_hr_review_and_admin_lock_and_exports_only_the_actors_rows()
    {
        using var app = Prepare();
        using var employee = app.Actor();
        using var hr = app.Actor("HR");
        using var admin = app.Actor("Admin");
        Assert.Equal(HttpStatusCode.Forbidden, (await Action(employee, "review")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Action(hr, "lock")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Action(admin, "lock")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Action(hr, "review")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Action(admin, "lock")).StatusCode);
        var own = await employee.GetFromJsonAsync<JsonElement>("/api/AttendancePeriod/2026/10/snapshot");
        Assert.Equal(1, own.GetProperty("data").GetProperty("data").GetProperty("version").GetInt32());
        var row = Assert.Single(own.GetProperty("data").GetProperty("data").GetProperty("rows").EnumerateArray());
        Assert.Equal(ApiFactory.EmployeeA, row.GetProperty("employeeId").GetGuid());
        var export = await employee.GetAsync("/api/AttendancePeriod/2026/10/export.xlsx");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.True(export.Headers.CacheControl?.NoStore);
        using var zip = new ZipArchive(new MemoryStream(await export.Content.ReadAsByteArrayAsync()));
        using var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = await reader.ReadToEndAsync();
        Assert.Contains("TEST-A", xml);
        Assert.DoesNotContain("TEST-B", xml);
        Assert.Equal(HttpStatusCode.Forbidden, (await Action(hr, "reopen")).StatusCode);
    }

    [Theory]
    [InlineData("attendance")]
    [InlineData("log")]
    [InlineData("leave")]
    [InlineData("overtime")]
    [InlineData("correction")]
    public async Task Locked_period_blocks_every_affected_persistence_path(string resource)
    {
        using var app = Prepare();
        using var admin = app.Actor("Admin");
        Assert.Equal(HttpStatusCode.OK, (await Action(admin, "review")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Action(admin, "lock")).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var attendance = await db.Attendances.FirstAsync(a => a.EmployeeId == ApiFactory.EmployeeA);
        switch (resource)
        {
            case "attendance": attendance.Note = "forbidden change"; break;
            case "log": db.AttendanceLogs.Add(new AttendanceLog { AttendanceId = attendance.Id, LogTime = app.Clock.GetUtcNow(), Type = AttendanceLogType.CheckIn }); break;
            case "leave": db.LeaveRequests.Add(new LeaveRequest { EmployeeId = ApiFactory.EmployeeA, FromDate = attendance.AttendanceDate, ToDate = attendance.AttendanceDate }); break;
            case "overtime": db.OvertimeRequests.Add(new OvertimeRequest { EmployeeId = ApiFactory.EmployeeA, WorkDate = attendance.AttendanceDate }); break;
            default: db.AttendanceCorrections.Add(new AttendanceCorrection { EmployeeId = ApiFactory.EmployeeA, WorkDate = attendance.AttendanceDate }); break;
        }
        var error = await Assert.ThrowsAsync<M.Core.Base.BaseException.ErrorException>(() => db.SaveChangesAsync());
        Assert.Equal("PERIOD_LOCKED", error.ErrorDetail.ErrorCode);
    }

    [Fact]
    public async Task Reopen_creates_a_new_snapshot_version_and_preserves_the_previous_snapshot_and_audit()
    {
        using var app = Prepare();
        using var admin = app.Actor("Admin");
        await Action(admin, "review"); await Action(admin, "lock");
        var first = await admin.GetStringAsync("/api/AttendancePeriod/2026/10/snapshot?version=1");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var rule = await db.AttendanceRules.SingleAsync(); rule.BreakMinutes = 30; await db.SaveChangesAsync();
        }
        Assert.Equal(first, await admin.GetStringAsync("/api/AttendancePeriod/2026/10/snapshot?version=1"));
        Assert.Equal(HttpStatusCode.OK, (await Action(admin, "reopen")).StatusCode);
        await Action(admin, "review");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            (await db.Attendances.FirstAsync()).Note = "Updated after review";
            await db.SaveChangesAsync();
            Assert.Equal(AttendancePeriodState.Open, (await db.AttendancePeriods.SingleAsync()).State);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Action(admin, "lock")).StatusCode);
        await Action(admin, "review"); await Action(admin, "lock");
        Assert.Equal(first, await admin.GetStringAsync("/api/AttendancePeriod/2026/10/snapshot?version=1"));
        var second = await admin.GetFromJsonAsync<JsonElement>("/api/AttendancePeriod/2026/10/snapshot?version=2");
        Assert.Equal(2, second.GetProperty("data").GetProperty("data").GetProperty("version").GetInt32());
        using var check = app.Services.CreateScope();
        var context = check.ServiceProvider.GetRequiredService<DatabaseContext>();
        Assert.Equal(2, await context.AttendancePeriodSnapshots.CountAsync());
        Assert.Contains(await context.AttendancePeriodEvents.Select(e => e.Action).ToListAsync(), action => action == "ReviewInvalidated");
        var snapshot = await context.AttendancePeriodSnapshots.FirstAsync();
        context.Remove(snapshot);
        Assert.Equal("IMMUTABLE_SNAPSHOT", (await Assert.ThrowsAsync<M.Core.Base.BaseException.ErrorException>(() => context.SaveChangesAsync())).ErrorDetail.ErrorCode);
    }

    [Fact]
    public async Task Unapproved_records_and_invalid_periods_cannot_be_closed()
    {
        using var app = Prepare();
        using var admin = app.Actor("Admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/AttendancePeriod/2026/13")).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            (await db.Attendances.FirstAsync()).ApprovalStatus = AttendanceApprovalStatus.Pending;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Action(admin, "review")).StatusCode);
    }

    private static Task<HttpResponseMessage> Action(HttpClient client, string action) =>
        client.PostAsJsonAsync($"/api/AttendancePeriod/2026/10/{action}", new { reason = "Đã đối soát và kiểm tra dữ liệu thử nghiệm." });

    [Theory]
    [InlineData("leave")]
    [InlineData("overtime")]
    [InlineData("correction")]
    public async Task Pending_requests_prevent_month_review(string resource)
    {
        using var app = Prepare();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var day = new DateTime(2026, 10, 17);
            switch (resource)
            {
                case "leave":
                    var type = new LeaveType { Name = "Nghỉ thử nghiệm", Code = "VERIFY", IsActive = true };
                    db.LeaveTypes.Add(type);
                    db.LeaveRequests.Add(new LeaveRequest { EmployeeId = ApiFactory.EmployeeA, LeaveTypeId = type.Id, FromDate = day, ToDate = day });
                    break;
                case "overtime": db.OvertimeRequests.Add(new OvertimeRequest { EmployeeId = ApiFactory.EmployeeA, WorkDate = day }); break;
                default: db.AttendanceCorrections.Add(new AttendanceCorrection { EmployeeId = ApiFactory.EmployeeA, WorkDate = day }); break;
            }
            await db.SaveChangesAsync();
        }
        using var admin = app.Actor("Admin");
        var response = await Action(admin, "review");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("PENDING_REQUESTS", await response.Content.ReadAsStringAsync());
    }

    private static ApiFactory Prepare()
    {
        var app = new ApiFactory(); app.Prepare();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Attendances.AddRange(new[] { ApiFactory.EmployeeA, ApiFactory.EmployeeB }.Select(employee => new Attendance
        {
            EmployeeId = employee, AttendanceDate = new DateTime(2026, 10, 17), Status = AttendanceStatus.Present,
            ApprovalStatus = AttendanceApprovalStatus.Approved, CheckInTime = DateTimeOffset.Parse("2026-10-17T01:00:00Z"),
            CheckOutTime = DateTimeOffset.Parse("2026-10-17T10:00:00Z")
        }));
        db.SaveChanges(); return app;
    }
}
