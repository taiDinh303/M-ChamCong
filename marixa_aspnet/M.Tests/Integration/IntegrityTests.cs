using System.Net;
using System.Net.Http.Json;
using M.API.Seed;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Contract.Services.Security;
using M.Core.Base;
using M.Core.Security;
using M.Services.Security;
using M.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class IntegrityTests
{
    [Fact]
    public async Task Seeder_preserves_existing_custom_rule()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using (var scope = app.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<AttendanceRule>();
            var rule = await repo.Entities.SingleAsync();
            rule.Code = "DEFAULT-CA";
            rule.CheckInTime = new TimeOnly(9, 0);
            rule.BreakMinutes = 45;
            await repo.SaveAsync();
        }
        await AttendanceRuleSeeder.SeedAsync(app.Services);
        using var check = app.Services.CreateScope();
        var result = await check.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<AttendanceRule>().Entities.SingleAsync();
        Assert.Equal(new TimeOnly(9, 0), result.CheckInTime);
        Assert.Equal(45, result.BreakMinutes);
    }

    [Fact]
    public async Task Unit_of_work_caches_repository_and_rolls_back_database_changes()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var scope = app.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<AttendanceRule>();
        Assert.Same(repo, uow.GetRepository<AttendanceRule>());
        await uow.BeginTransactionAsync();
        var rule = await repo.Entities.SingleAsync();
        rule.Name = "Should roll back";
        await uow.SaveAsync();
        await uow.RollBackAsync();
        Assert.Equal("Test rule", (await repo.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task Last_active_admin_cannot_be_deactivated()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var scope = app.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var admin = new ApplicationUser { UserName = "test-admin", SecurityStamp = Guid.NewGuid().ToString() };
        var role = new ApplicationRole { Name = "Admin" };
        await uow.GetRepository<ApplicationUser>().InsertAsync(admin);
        await uow.GetRepository<ApplicationRole>().InsertAsync(role);
        await uow.GetRepository<ApplicationUserRole>().InsertAsync(new() { UserId = admin.Id, RoleId = role.Id });
        await uow.SaveAsync();
        var guard = new AdminAccountGuard(uow, new AdminActor(), app.Clock);
        var invoked = false;
        var error = await Assert.ThrowsAsync<BaseException.ErrorException>(() => guard.ProtectDeactivationAsync(admin.Id,
            () => { invoked = true; return Task.CompletedTask; }));
        Assert.Equal("LAST_ACTIVE_ADMIN", error.ErrorDetail.ErrorCode);
        Assert.False(invoked);
    }

    [Fact]
    public async Task Duplicate_active_attendance_day_is_rejected_by_database()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var scope = app.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<Attendance>();
        var day = new DateTime(2026, 10, 17);
        await repo.InsertAsync(new Attendance { EmployeeId = ApiFactory.EmployeeA, AttendanceDate = day });
        await uow.SaveAsync();
        await repo.InsertAsync(new Attendance { EmployeeId = ApiFactory.EmployeeA, AttendanceDate = day });
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveAsync());
    }

    [Fact]
    public async Task Activating_a_rule_replaces_the_previous_active_rule_atomically()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var admin = app.Actor("Admin");
        var response = await admin.PostAsJsonAsync("/api/AttendanceRule/create", new
        {
            code = "NEW", name = "Replacement", standardHours = 8,
            checkInTime = "09:00:00", checkOutTime = "18:00:00", breakMinutes = 60, isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = app.Services.CreateScope();
        var rules = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<AttendanceRule>().Entities.ToListAsync();
        Assert.Equal("NEW", Assert.Single(rules, r => r.IsActive).Code);
        Assert.False(rules.Single(r => r.Code == "TEST").IsActive);
    }

    [Fact]
    public async Task Correction_cannot_target_another_employees_attendance()
    {
        using var app = new ApiFactory();
        app.Prepare();
        var attendance = new Attendance { EmployeeId = ApiFactory.EmployeeB, AttendanceDate = new DateTime(2026, 10, 17) };
        using (var scope = app.Services.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await uow.GetRepository<Attendance>().InsertAsync(attendance);
            await uow.SaveAsync();
        }
        using var employee = app.Actor();
        var response = await employee.PostAsJsonAsync("/api/AttendanceCorrection/create", new
        {
            employeeId = ApiFactory.EmployeeA, attendanceId = attendance.Id, workDate = "2026-10-17",
            proposedCheckIn = "2026-10-17T01:00:00Z", reason = "Test ownership validation"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class AdminActor : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => ApiFactory.UserA;
        public Guid? EmployeeId => ApiFactory.EmployeeA;
        public string Name => "test-admin";
        public bool IsInRole(string role) => role == "Admin";
    }
}
