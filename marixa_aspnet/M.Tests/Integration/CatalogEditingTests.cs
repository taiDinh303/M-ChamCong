using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using M.Contract.Repositories.Entities;
using M.Repositories.Context;
using M.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class CatalogEditingTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("HR")]
    public async Task Staff_edit_has_atomic_history_stale_detection_and_archive(string role)
    {
        using var app = new ApiFactory(); app.Prepare(); using var client = app.Actor(role);
        client.DefaultRequestHeaders.Add("X-Change-Reason", Uri.EscapeDataString("Điều chỉnh cơ cấu"));
        var created = await client.PostAsJsonAsync("/api/Department/create", new { code = "OPS", name = "Operations", isActive = true });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var row = await db.Departments.AsNoTracking().SingleAsync(x => x.Code == "OPS");
        client.DefaultRequestHeaders.Add("X-Record-Version", row.LastUpdatedTime.ToString("O"));
        var body = new { row.Id, row.Code, name = "People operations", isActive = true };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/Department/update", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/Department/update", body)).StatusCode);
        var history = await db.AuditLogs.AsNoTracking().Where(x => x.EntityId == row.Id).ToListAsync();
        Assert.Equal(2, history.Count);
        var changed = Assert.Single(history, x => x.Action == AuditAction.Update);
        Assert.Contains("Operations", changed.BeforeJson!); Assert.Contains("People operations", changed.AfterJson!);
        Assert.Equal("Điều chỉnh cơ cấu", changed.Reason);
        var current = await db.Departments.AsNoTracking().SingleAsync(x => x.Id == row.Id);
        client.DefaultRequestHeaders.Remove("X-Record-Version");
        client.DefaultRequestHeaders.Add("X-Record-Version", current.LastUpdatedTime.ToString("O"));
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/Department/soft-delete/{row.Id}")).StatusCode);
        Assert.NotNull((await db.Departments.AsNoTracking().SingleAsync(x => x.Id == row.Id)).DeletedTime);
        var archived = await client.GetAsync("/api/Department/get-archived");
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        Assert.Contains("People operations", await archived.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Accountant")]
    public async Task Non_staff_cannot_write_any_catalog_or_contract(string role)
    {
        using var app = new ApiFactory(); app.Prepare(); using var client = app.Actor(role);
        foreach (var entity in new[] { "Department", "Position", "Bank", "LeaveType", "HolidayCalendar", "EmployeeContract" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/{entity}/create", new { })).StatusCode);
    }

    [Fact]
    public async Task Reason_required_and_contract_employee_cannot_change()
    {
        using var app = new ApiFactory(); app.Prepare(); using var client = app.Actor("HR");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/Employee/delete/{ApiFactory.EmployeeB}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Department/create", new { code = "A", name = "A" })).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var row = new EmployeeContract { EmployeeId = ApiFactory.EmployeeA, ContractNumber = "C-01", ContractType = ContractType.FixedTerm, StartDate = new DateTime(2026,1,1) };
        db.EmployeeContracts.Add(row); await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-Change-Reason", "Correct contract details");
        client.DefaultRequestHeaders.Add("X-Record-Version", row.LastUpdatedTime.ToString("O"));
        var result = await client.PutAsJsonAsync("/api/EmployeeContract/update", new { row.Id, employeeId = ApiFactory.EmployeeB, row.ContractNumber, contractType = 2, row.StartDate });
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("IMMUTABLE_EMPLOYEE", JsonDocument.Parse(await result.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString());
        Assert.Equal(ApiFactory.EmployeeA, (await db.EmployeeContracts.AsNoTracking().SingleAsync()).EmployeeId);
    }

    [Theory]
    [InlineData("Position")]
    [InlineData("Bank")]
    [InlineData("LeaveType")]
    [InlineData("HolidayCalendar")]
    public async Task Hr_can_create_and_read_each_remaining_catalog(string kind)
    {
        using var app = new ApiFactory(); app.Prepare(); using var client = app.Actor("HR");
        client.DefaultRequestHeaders.Add("X-Change-Reason", "Initial organization setup");
        object body = kind == "HolidayCalendar"
            ? new { date = "2027-01-01", year = 2027, type = 2, name = "New year", isActive = true }
            : new { code = "NEW", name = "New catalog record", isActive = true };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/{kind}/create", body)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/{kind}/get-all")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/{kind}/get-archived")).StatusCode);
    }
}
