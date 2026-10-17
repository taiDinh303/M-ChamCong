using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using M.Tests.Infrastructure;

namespace M.Tests.Integration;

public sealed class LeaveLedgerValidationTests
{
    [Fact]
    public async Task Http_contract_accepts_signed_adjustments_and_rejects_invalid_grants_and_zero()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var admin = app.Actor("Admin");
        var adjusted = await admin.PostAsJsonAsync("/api/LeaveLedger/create-entry", new
        {
            employeeId = ApiFactory.EmployeeB, year = 2026, entryType = 2, days = -0.5m,
            reason = "Isolated audited correction"
        });
        Assert.Equal(HttpStatusCode.OK, adjusted.StatusCode);
        var entries = await admin.GetFromJsonAsync<JsonElement>($"/api/LeaveLedger/by-employee/{ApiFactory.EmployeeB}/year/2026");
        Assert.Equal(-0.5m, entries.GetProperty("data")[0].GetProperty("days").GetDecimal());

        foreach (var invalid in new[] { (Type: 1, Days: -0.5m), (Type: 1, Days: 0m), (Type: 2, Days: 0m), (Type: 2, Days: -1000m), (Type: 1, Days: 1000m) })
        {
            var result = await admin.PostAsJsonAsync("/api/LeaveLedger/create-entry", new
            {
                employeeId = ApiFactory.EmployeeB, year = 2026, entryType = invalid.Type, days = invalid.Days,
                reason = "Invalid isolated test entry"
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}
