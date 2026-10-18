using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using M.API.Seed;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using M.Repositories.Context;
using M.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class DemoScenarioTests
{
    [Fact]
    public async Task Dataset_is_additive_idempotent_and_keeps_demo_accounts_unprivileged()
    {
        using var app = new ApiFactory(); app.Prepare();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var name in new[] { "Admin", "Employee" })
            Assert.True((await roles.CreateAsync(new ApplicationRole { Name = name })).Succeeded);
        var bootstrap = new ApplicationUser { UserName = "bootstrap-without-profile" };
        Assert.True((await users.CreateAsync(bootstrap, Guid.NewGuid() + "aA1!")).Succeeded);
        Assert.True((await users.AddToRoleAsync(bootstrap, "Admin")).Succeeded);
        var owner = new ApplicationUser { UserName = "demo-test-owner", EmployeeId = ApiFactory.EmployeeA };
        var password = Guid.NewGuid() + "aA1!";
        Assert.True((await users.CreateAsync(owner, password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(owner, "Admin")).Succeeded);
        var original = await db.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).ToListAsync();
        Assert.True(await DemoScenarioSeeder.SeedAsync(db, users, password));
        Assert.False(await DemoScenarioSeeder.SeedAsync(db, users, password));
        Assert.Equal(original.Count + 6, await db.Employees.CountAsync());
        foreach (var existing in original)
        {
            var after = await db.Employees.AsNoTracking().SingleAsync(e => e.Id == existing.Id);
            Assert.Equal(existing.EmployeeCode, after.EmployeeCode);
            Assert.Equal(existing.UserId, after.UserId);
            Assert.Equal(existing.Status, after.Status);
        }
        foreach (var account in DemoScenarioSeeder.AccountNames)
            Assert.Equal(new[] { "Employee" }, await users.GetRolesAsync((await users.FindByNameAsync(account))!));
        Assert.Equal(73, await db.Attendances.CountAsync());
        Assert.Equal(143, await db.AttendanceLogs.CountAsync());
        Assert.Equal(12, await db.OvertimeRequests.CountAsync());
        Assert.Equal(4, await db.LeaveRequests.CountAsync());
        Assert.Equal(10, await db.LeaveLedger.CountAsync());
        Assert.Equal(AttendancePeriodState.Locked, (await db.AttendancePeriods.SingleAsync()).State);
        var seededSnapshot = Assert.Single(await db.AttendancePeriodSnapshots.ToListAsync());
        Assert.Matches("^[0-9A-F]{64}$", seededSnapshot.Sha256);
        using var demo = app.Actor("Employee", DemoScenarioSeeder.Id("employee-0"), DemoScenarioSeeder.Id("user-0"));
        var snapshot = await demo.GetFromJsonAsync<JsonElement>("/api/AttendancePeriod/2026/9/snapshot?version=1");
        var ownRow = Assert.Single(snapshot.GetProperty("data").GetProperty("data").GetProperty("rows").EnumerateArray());
        Assert.Equal(DemoScenarioSeeder.Id("employee-0"), ownRow.GetProperty("employeeId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await demo.GetAsync("/api/AttendancePeriod/2026/9/export.xlsx?version=1")).StatusCode);
        Assert.Equal(2, await db.AttendancePeriodEvents.CountAsync());
        var missingCheckout = await db.Attendances.SingleAsync(a => a.Id == DemoScenarioSeeder.Id("attendance-2-16"));
        Assert.Null(missingCheckout.ActualHours);
        var correction = await db.AttendanceCorrections.SingleAsync();
        Assert.Equal(missingCheckout.Id, correction.AttendanceId);
        Assert.All(await db.EmployeePromotions.ToListAsync(), p => Assert.Null(p.AppliedAt));
        Assert.All(await db.EmployeeHandovers.ToListAsync(), p => Assert.Null(p.AppliedAt));
        var closed = new Attendance { EmployeeId = DemoScenarioSeeder.Id("employee-1"), AttendanceDate = new DateTime(2026, 9, 29) };
        db.Attendances.Add(closed);
        await Assert.ThrowsAsync<M.Core.Base.BaseException.ErrorException>(() => db.SaveChangesAsync());
    }
}
