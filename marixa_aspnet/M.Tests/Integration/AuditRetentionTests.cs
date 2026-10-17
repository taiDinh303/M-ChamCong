using System.Net;
using M.Contract.Repositories.Entities;
using M.Repositories.Context;
using M.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class AuditRetentionTests
{
    [Fact]
    public async Task Raw_event_cannot_be_modified_or_removed_even_in_an_open_period()
    {
        using var app = new ApiFactory(); app.Prepare();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var attendance = new Attendance { EmployeeId = ApiFactory.EmployeeA, AttendanceDate = new DateTime(2026, 10, 17) };
        var original = app.Clock.GetUtcNow();
        var log = new AttendanceLog { AttendanceId = attendance.Id, LogTime = original, Type = AttendanceLogType.CheckIn };
        db.Attendances.Add(attendance); db.AttendanceLogs.Add(log); await db.SaveChangesAsync();
        log.LogTime = original.AddHours(1);
        var error = await Assert.ThrowsAsync<M.Core.Base.BaseException.ErrorException>(() => db.SaveChangesAsync());
        Assert.Equal("IMMUTABLE_ATTENDANCE_LOG", error.ErrorDetail.ErrorCode);
        db.ChangeTracker.Clear();
        Assert.Equal(original, (await db.AttendanceLogs.SingleAsync()).LogTime);
        db.Remove(await db.AttendanceLogs.SingleAsync());
        Assert.Equal("IMMUTABLE_ATTENDANCE_LOG", (await Assert.ThrowsAsync<M.Core.Base.BaseException.ErrorException>(() => db.SaveChangesAsync())).ErrorDetail.ErrorCode);
    }

    [Fact]
    public async Task Approved_records_cannot_be_deleted_and_pdf_is_scoped_to_owner_or_hr_admin()
    {
        using var app = new ApiFactory(); app.Prepare();
        Guid attendanceId, leaveId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var type = new LeaveType { Code = "VERIFY", Name = "Phép năm", IsActive = true };
            var attendance = new Attendance { EmployeeId = ApiFactory.EmployeeA, AttendanceDate = new DateTime(2026, 10, 17), ApprovalStatus = AttendanceApprovalStatus.Approved };
            var leave = new LeaveRequest { EmployeeId = ApiFactory.EmployeeA, LeaveTypeId = type.Id, FromDate = new DateTime(2026, 10, 18), ToDate = new DateTime(2026, 10, 18), Status = LeaveRequestStatus.Approved };
            db.LeaveTypes.Add(type); db.Attendances.Add(attendance); db.LeaveRequests.Add(leave); await db.SaveChangesAsync();
            attendanceId = attendance.Id; leaveId = leave.Id;
        }
        using var admin = app.Actor("Admin");
        foreach (var path in new[] { $"Attendance/delete/{attendanceId}", $"Attendance/soft-delete/{attendanceId}", $"LeaveRequest/delete/{leaveId}", $"LeaveRequest/soft-delete/{leaveId}" })
            Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync("/api/" + path)).StatusCode);
        using var owner = app.Actor();
        using var other = app.Actor(employee: ApiFactory.EmployeeB);
        var pdf = await owner.GetAsync($"/api/LeaveRequest/{leaveId}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.True(pdf.Headers.CacheControl?.NoStore);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/LeaveRequest/{leaveId}/pdf")).StatusCode);
        using var hr = app.Actor("HR", ApiFactory.EmployeeB);
        Assert.Equal(HttpStatusCode.OK, (await hr.GetAsync($"/api/LeaveRequest/{leaveId}/pdf")).StatusCode);
        using var check = app.Services.CreateScope();
        var context = check.ServiceProvider.GetRequiredService<DatabaseContext>();
        Assert.Null((await context.LeaveRequests.SingleAsync()).DeletedTime);
        Assert.Null((await context.Attendances.SingleAsync()).DeletedTime);
    }
}
