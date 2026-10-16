using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using M.Repositories.Context;
using M.Contract.Repositories.Entities;
using M.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class AuthorizationTests
{
    [Fact]
    public void Every_business_contract_resolves_from_the_composition_root()
    {
        using var app = new ApiFactory();
        using var scope = app.Services.CreateScope();
        var contracts = typeof(M.Contract.Services.Interface.IAttendanceService).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Service", StringComparison.Ordinal));
        foreach (var contract in contracts)
            Assert.NotNull(scope.ServiceProvider.GetRequiredService(contract));
    }

    [Theory]
    [InlineData("/api/Attendance/by-employee/")]
    [InlineData("/api/EmployeeSalary/by-employee/")]
    [InlineData("/api/EmployeeBankAccount/by-employee/")]
    [InlineData("/api/OvertimeRequest/by-employee/")]
    public async Task Employee_cannot_read_other_employees(string route)
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var client = app.Actor();
        var result = await client.GetAsync(route + ApiFactory.EmployeeB);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData("/api/Attendance/get-all")]
    [InlineData("/api/LeaveRequest/get-all")]
    [InlineData("/api/EmployeeReport/get-all")]
    public async Task Employee_cannot_enumerate_staff_data(string route)
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var client = app.Actor();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(route)).StatusCode);
    }

    [Fact]
    public async Task Checkin_enforces_owner_and_Vietnam_business_day_without_photo()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var client = app.Actor();
        var denied = await client.PostAsJsonAsync("/api/Attendance/checkin", new { employeeId = ApiFactory.EmployeeB, type = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var result = await client.PostAsJsonAsync("/api/Attendance/checkin", new { employeeId = ApiFactory.EmployeeA, type = 1, latitude = 10.77m, longitude = 106.7m, deviceId = "test-device" });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        using var document = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
        Assert.StartsWith("2026-10-17", document.RootElement.GetProperty("data").GetProperty("attendance").GetProperty("attendanceDate").GetString());
        var repeat = await client.PostAsJsonAsync("/api/Attendance/checkin", new { employeeId = ApiFactory.EmployeeA, type = 1 });
        using var repeated = JsonDocument.Parse(await repeat.Content.ReadAsStringAsync());
        Assert.True(repeated.RootElement.GetProperty("data").GetProperty("alreadyRecorded").GetBoolean());
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        Assert.Single(db.Attendances);
        var log = Assert.Single(db.AttendanceLogs);
        Assert.Equal(10.77m, log.Latitude);
        Assert.Equal("test-device", log.DeviceId);
    }

    [Fact]
    public async Task Anonymous_cannot_change_password_or_register()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        foreach (var route in new[] { "/api/Auth/change-password", "/api/Auth/register" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(route, new { })).StatusCode);
    }

    [Fact]
    public async Task Staff_can_read_employee_data_and_liveness_is_public()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var staff = app.Actor("HR");
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/Attendance/by-employee/" + ApiFactory.EmployeeB)).StatusCode);
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/missing-route")).StatusCode);
    }

    [Fact]
    public async Task Readiness_fails_when_business_schema_is_missing()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Negative_pagination_is_rejected()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var client = app.Actor("HR");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/Attendance/get-all?pageSize=-1")).StatusCode);
    }
}
