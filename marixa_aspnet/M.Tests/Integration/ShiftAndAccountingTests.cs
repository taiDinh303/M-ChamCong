using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using M.Contract.Repositories.Entities;
using M.Repositories.Context;
using M.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class ShiftAndAccountingTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("HR")]
    public async Task Shift_editor_creates_and_disables_without_deleting_history(string role)
    {
        using var app = new ApiFactory(); app.Prepare();
        using var client = app.Actor(role);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Shift/create", ShiftPayload())).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var shift = await db.Shifts.SingleAsync();
        db.EmployeeShifts.Add(new EmployeeShift { EmployeeId = ApiFactory.EmployeeA, ShiftId = shift.Id,
            EffectiveFrom = new DateTime(2026, 10, 26), EffectiveTo = new DateTime(2026, 10, 26) });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/Shift/update", new {
            shift.Id, shift.Code, shift.Name, startTime = "08:00:00", endTime = "17:00:00",
            shift.StandardHours, shift.BreakMinutes, shift.WorkDays, isActive = false })).StatusCode);
        db.ChangeTracker.Clear();
        Assert.False((await db.Shifts.SingleAsync()).IsActive);
        Assert.Single(await db.EmployeeShifts.ToListAsync());
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Manager")]
    [InlineData("Accountant")]
    public async Task Other_roles_cannot_create_shifts(string role)
    {
        using var app = new ApiFactory(); app.Prepare(); using var client = app.Actor(role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Shift/create", ShiftPayload())).StatusCode);
    }

    [Fact]
    public async Task Equal_shift_times_are_rejected_on_create_and_update_but_overnight_is_allowed()
    {
        using var app = new ApiFactory(); app.Prepare(); using var client = app.Actor("HR");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Shift/create", ShiftPayload("08:00:00", "08:00:00"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Shift/create", ShiftPayload("22:00:00", "06:00:00"))).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var shift = await db.Shifts.SingleAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/Shift/update", new {
            shift.Id, shift.Code, shift.Name, startTime = "08:00:00", endTime = "08:00:00",
            shift.StandardHours, shift.BreakMinutes, shift.WorkDays, isActive = true })).StatusCode);
        db.ChangeTracker.Clear(); Assert.Equal(new TimeOnly(22, 0), (await db.Shifts.SingleAsync()).StartTime);
    }

    [Fact]
    public async Task Weekly_assignment_skips_sunday_and_retry_does_not_duplicate_rows()
    {
        using var app = new ApiFactory(); app.Prepare(); using var client = app.Actor("HR");
        var shift = await SeedShift(app);
        var third = Guid.NewGuid();
        using (var scope = app.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Employees.Add(new Employee { Id = third, EmployeeCode = "ZZ-C", GivenName = "zz-C", FamilyName = "Test", Status = EmployeeStatus.Working });
            await db.SaveChangesAsync();
        }
        var payload = new { employeeIds = new[] { ApiFactory.EmployeeA, ApiFactory.EmployeeB, third }, shiftId = shift,
            effectiveFrom = "2026-10-26", effectiveTo = "2026-11-01", skipSunday = true };
        var first = await client.PostAsJsonAsync("/api/EmployeeShift/create-bulk", payload);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(18, (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("createdAssignmentCount").GetInt32());
        var repeat = await client.PostAsJsonAsync("/api/EmployeeShift/create-bulk", payload);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Equal(0, (await repeat.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("createdAssignmentCount").GetInt32());
        using var read = app.Services.CreateScope();
        var rows = await read.ServiceProvider.GetRequiredService<DatabaseContext>().EmployeeShifts.ToListAsync();
        Assert.Equal(18, rows.Count);
        Assert.All(rows, row => { Assert.NotEqual(DayOfWeek.Sunday, row.EffectiveFrom.DayOfWeek); Assert.Equal(row.EffectiveFrom, row.EffectiveTo); });
    }

    [Fact]
    public async Task Single_employee_allows_31_days_and_sundays_but_rejects_32_days_without_writes()
    {
        using var app = new ApiFactory(); app.Prepare(); using var client = app.Actor("HR"); var shift = await SeedShift(app);
        var rejected = await client.PostAsJsonAsync("/api/EmployeeShift/create-bulk", new { employeeIds = new[] { ApiFactory.EmployeeA },
            shiftId = shift, effectiveFrom = "2026-12-01", effectiveTo = "2027-01-01", skipSunday = false });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        using (var scope = app.Services.CreateScope()) Assert.Empty(await scope.ServiceProvider.GetRequiredService<DatabaseContext>().EmployeeShifts.ToListAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/EmployeeShift/create-bulk", new { employeeIds = new[] { ApiFactory.EmployeeA },
            shiftId = shift, effectiveFrom = "2026-12-01", effectiveTo = "2026-12-31", skipSunday = false })).StatusCode);
        using var read = app.Services.CreateScope(); var rows = await read.ServiceProvider.GetRequiredService<DatabaseContext>().EmployeeShifts.ToListAsync();
        Assert.Equal(31, rows.Count); Assert.Contains(rows, row => row.EffectiveFrom.DayOfWeek == DayOfWeek.Sunday);
    }

    [Fact]
    public async Task Manager_cannot_schedule_outside_scope_or_disable_a_shared_shift()
    {
        using var app = new ApiFactory(); app.Prepare(); var shift = await SeedShift(app);
        using var client = app.Actor("Manager", ApiFactory.EmployeeB);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/EmployeeShift/create-bulk", new { employeeIds = new[] { ApiFactory.EmployeeA },
            shiftId = shift, effectiveFrom = "2026-10-26", effectiveTo = "2026-10-26" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/Shift/update", new { id = shift, code = "ZZ-SHIFT", name = "zz-Ca thử",
            startTime = "08:00:00", endTime = "17:00:00", standardHours = 8, breakMinutes = 60, workDays = 127, isActive = false })).StatusCode);
    }

    [Theory]
    [InlineData("Accountant", true)]
    [InlineData("Admin", true)]
    [InlineData("HR", false)]
    [InlineData("Manager", false)]
    [InlineData("Employee", false)]
    public async Task Company_payroll_read_permission_is_explicit_and_accountant_remains_read_only(string role, bool allowed)
    {
        using var app = new ApiFactory(); app.Prepare();
        var id = Guid.NewGuid();
        using (var scope = app.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Payrolls.Add(new Payroll { Id = id, EmployeeId = ApiFactory.EmployeeB, PayrollMonth = new DateTime(2026, 10, 1), BasicSalary = 9000000, NetSalary = 8000000 });
            await db.SaveChangesAsync();
        }
        using var client = app.Actor(role);
        var expected = allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
        Assert.Equal(expected, (await client.GetAsync("/api/Payroll/get-all")).StatusCode);
        Assert.Equal(expected, (await client.GetAsync($"/api/Payroll/get-by-id/{id}")).StatusCode);
        if (role == "Accountant") {
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Payroll/create", new { employeeId = ApiFactory.EmployeeA, payrollMonth = "2026-11-01" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/Payroll/update", new { id, employeeId = ApiFactory.EmployeeB, payrollMonth = "2026-10-01" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/Payroll/delete/{id}")).StatusCode);
        }
    }

    private static object ShiftPayload(string start = "08:00:00", string end = "17:00:00") => new {
        code = "ZZ-SHIFT", name = "zz-Ca thử", startTime = start, endTime = end,
        standardHours = 8, breakMinutes = 60, workDays = 127, isActive = true };
    private static async Task<Guid> SeedShift(ApiFactory app) {
        using var client = app.Actor("HR");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Shift/create", ShiftPayload())).StatusCode);
        using var scope = app.Services.CreateScope(); return (await scope.ServiceProvider.GetRequiredService<DatabaseContext>().Shifts.SingleAsync()).Id;
    }
}
