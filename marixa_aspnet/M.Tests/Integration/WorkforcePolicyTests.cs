using System.Net;
using System.Net.Http.Json;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using M.Contract.Services.Interface;
using M.Repositories.Context;
using M.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class WorkforcePolicyTests
{
    [Fact]
    public async Task Leave_create_approve_and_cancel_preserve_balance_and_prevent_self_approval()
    {
        using var app = new ApiFactory(); app.Prepare();
        using var setup = app.Services.CreateScope();
        var users = setup.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = setup.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        Assert.True((await roles.CreateAsync(new ApplicationRole { Name = "HR" })).Succeeded);
        var approver = new ApplicationUser { UserName = "hr-reviewer" };
        Assert.True((await users.CreateAsync(approver, Guid.NewGuid() + "Aa1!")).Succeeded);
        Assert.True((await users.AddToRoleAsync(approver, "HR")).Succeeded);
        var db = setup.ServiceProvider.GetRequiredService<DatabaseContext>();
        (await db.Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeB)).UserId = approver.Id;
        var type = new LeaveType { Code = "LIMITED", Name = "Phép thử", MaxDays = 12, IsActive = true };
        db.LeaveTypes.Add(type); await db.SaveChangesAsync();
        using var owner = app.Actor();
        var create = await owner.PostAsJsonAsync("/api/LeaveRequest/create", new { employeeId = ApiFactory.EmployeeA, leaveTypeId = type.Id, fromDate = "2026-10-18", toDate = "2026-10-18", totalDays = 0.5m });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var id = (await db.LeaveRequests.SingleAsync()).Id;
        using var self = app.Actor("HR");
        Assert.Equal(HttpStatusCode.Forbidden, (await self.PostAsJsonAsync($"/api/LeaveRequest/approve/{id}", new { })).StatusCode);
        using var hr = app.Actor("HR", ApiFactory.EmployeeB, approver.Id);
        Assert.Equal(HttpStatusCode.OK, (await hr.PostAsJsonAsync($"/api/LeaveRequest/approve/{id}", new { note = "Đã đối soát." })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await hr.PostAsJsonAsync($"/api/LeaveRequest/approve/{id}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await hr.PostAsJsonAsync($"/api/LeaveRequest/cancel/{id}", new { reason = "Đã xác nhận nhân viên đi làm." })).StatusCode);
        var entries = await db.LeaveLedger.ToListAsync();
        Assert.Equal(0.5m, Assert.Single(entries, e => e.EntryType == LeaveLedgerEntryType.Deduct).Days);
        Assert.Equal(0.5m, Assert.Single(entries, e => e.EntryType == LeaveLedgerEntryType.Refund).Days);
    }

    [Theory]
    [InlineData("Manager", HttpStatusCode.Forbidden)]
    [InlineData("HR", HttpStatusCode.OK)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task Only_hr_admin_may_review_their_own_attendance(string role, HttpStatusCode expected)
    {
        using var app = new ApiFactory(); app.Prepare();
        using var setup = app.Services.CreateScope();
        var db = setup.ServiceProvider.GetRequiredService<DatabaseContext>();
        var record = new Attendance { EmployeeId = ApiFactory.EmployeeA, AttendanceDate = new DateTime(2026, 10, 17) };
        db.Attendances.Add(record); await db.SaveChangesAsync();
        using var client = app.Actor(role);
        var response = await client.PostAsJsonAsync("/api/Attendance/approve", new { id = record.Id, approvalStatus = 1, status = 1, approvedBy = ApiFactory.EmployeeB });
        Assert.Equal(expected, response.StatusCode);
        db.ChangeTracker.Clear();
        if (expected == HttpStatusCode.OK) Assert.Equal(ApiFactory.EmployeeA, (await db.Attendances.SingleAsync()).ApprovedBy);
    }

    [Fact]
    public async Task Approved_leave_requires_another_hr_admin_and_reason_and_refunds_only_once()
    {
        using var app = new ApiFactory(); app.Prepare();
        using var setup = app.Services.CreateScope();
        var db = setup.ServiceProvider.GetRequiredService<DatabaseContext>();
        var type = new LeaveType { Code = "ANNUAL", Name = "Phép năm", MaxDays = 12, IsActive = true };
        var leave = new LeaveRequest { EmployeeId = ApiFactory.EmployeeA, LeaveTypeId = type.Id, FromDate = new DateTime(2026, 10, 18), ToDate = new DateTime(2026, 10, 18), Status = LeaveRequestStatus.Approved, TotalDays = 1, ApprovedBy = ApiFactory.EmployeeB, ApprovedAt = new DateTime(2026, 10, 17) };
        db.LeaveTypes.Add(type); db.LeaveRequests.Add(leave);
        db.LeaveLedger.Add(new LeaveLedger { EmployeeId = ApiFactory.EmployeeA, LeaveTypeId = type.Id, LeaveRequestId = leave.Id, EntryType = LeaveLedgerEntryType.Deduct, Days = 1, Year = 2026 });
        await db.SaveChangesAsync();
        using var owner = app.Actor("HR");
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync($"/api/LeaveRequest/cancel/{leave.Id}", new { reason = "Không còn nhu cầu nghỉ." })).StatusCode);
        using var reviewer = app.Actor("HR", ApiFactory.EmployeeB);
        Assert.Equal(HttpStatusCode.BadRequest, (await reviewer.PostAsJsonAsync($"/api/LeaveRequest/cancel/{leave.Id}", new { reason = "" })).StatusCode);
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.OK, (await reviewer.PostAsJsonAsync($"/api/LeaveRequest/cancel/{leave.Id}", new { reason = "Nhân viên đã trở lại làm việc." })).StatusCode);
        db.ChangeTracker.Clear();
        var row = await db.LeaveRequests.SingleAsync();
        Assert.Equal(LeaveRequestStatus.Cancelled, row.Status);
        Assert.Equal(new DateTime(2026, 10, 17), row.ApprovedAt);
        Assert.Equal(ApiFactory.EmployeeB, row.ApprovedBy);
        Assert.Equal(ApiFactory.EmployeeB, row.CancelledBy);
        Assert.Equal(app.Clock.GetUtcNow().UtcDateTime, row.CancelledAt);
        Assert.Equal(1, Assert.Single(await db.LeaveLedger.Where(l => l.EntryType == LeaveLedgerEntryType.Refund).ToListAsync()).Days);
    }

    [Fact]
    public async Task Future_promotion_applies_at_vietnam_midnight_and_only_once()
    {
        using var app = new ApiFactory(); app.Prepare();
        using var setup = app.Services.CreateScope();
        var db = setup.ServiceProvider.GetRequiredService<DatabaseContext>();
        var position = new Position { Code = "LEAD", Name = "Trưởng nhóm", IsActive = true };
        db.Positions.Add(position);
        (await db.Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeA)).ManagerId = ApiFactory.EmployeeB;
        await db.SaveChangesAsync();
        using var owner = app.Actor();
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync("/api/EmployeePromotion/request", new { newPositionId = position.Id, targetRoleName = "Employee", reason = "Đủ điều kiện thăng chức.", effectiveDate = "2026-10-18" })).StatusCode);
        var id = (await db.EmployeePromotions.SingleAsync()).Id;
        using var manager = app.Actor("Manager", ApiFactory.EmployeeB, ApiFactory.EmployeeB);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsJsonAsync($"/api/EmployeePromotion/review/{id}", new { approve = true })).StatusCode);
        db.ChangeTracker.Clear();
        Assert.Null((await db.Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeA)).PositionId);
        var processor = setup.ServiceProvider.GetRequiredService<IEmploymentChangeProcessor>();
        Assert.Equal(0, await processor.ApplyDueAsync());
        app.Clock.Now = DateTimeOffset.Parse("2026-10-17T17:00:00Z");
        Assert.Equal(1, await processor.ApplyDueAsync());
        Assert.Equal(0, await processor.ApplyDueAsync());
        Assert.Equal(position.Id, (await db.Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeA)).PositionId);
        Assert.Equal(app.Clock.GetUtcNow().UtcDateTime, (await db.EmployeePromotions.SingleAsync()).AppliedAt);
    }

    [Fact]
    public async Task Scheduled_departure_locks_account_but_keeps_last_admin_until_replacement_exists()
    {
        using var app = new ApiFactory(); app.Prepare();
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        Assert.True((await roles.CreateAsync(new ApplicationRole { Name = "Admin" })).Succeeded);
        var admin = new ApplicationUser { Id = ApiFactory.UserA, UserName = "departing-admin", LockoutEnabled = true };
        Assert.True((await users.CreateAsync(admin, Guid.NewGuid() + "Aa1!")).Succeeded);
        Assert.True((await users.AddToRoleAsync(admin, "Admin")).Succeeded);
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        (await db.Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeA)).UserId = admin.Id;
        db.EmployeeHandovers.Add(new EmployeeHandover { EmployeeId = ApiFactory.EmployeeA, ManagerId = ApiFactory.EmployeeB, LastWorkingDate = new DateTime(2026, 10, 16), Reason = "Dữ liệu thử nghiệm", Status = EmployeeHandoverStatus.Approved });
        await db.SaveChangesAsync();
        var processor = scope.ServiceProvider.GetRequiredService<IEmploymentChangeProcessor>();
        Assert.Equal(0, await processor.ApplyDueAsync());
        Assert.Null((await db.EmployeeHandovers.SingleAsync()).AppliedAt);
        var replacement = new ApplicationUser { UserName = "replacement-admin", LockoutEnabled = true };
        Assert.True((await users.CreateAsync(replacement, Guid.NewGuid() + "Aa1!")).Succeeded);
        Assert.True((await users.AddToRoleAsync(replacement, "Admin")).Succeeded);
        Assert.Equal(1, await processor.ApplyDueAsync());
        Assert.True(await users.IsLockedOutAsync((await users.FindByIdAsync(admin.Id.ToString()))!));
        Assert.Equal(EmployeeStatus.Resigned, (await db.Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeA)).Status);
    }
}
