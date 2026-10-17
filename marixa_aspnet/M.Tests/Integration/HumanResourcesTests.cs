using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using M.Contract.Repositories.Entities;
using M.Repositories.Context;
using M.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class HumanResourcesTests
{
    [Fact]
    public async Task Report_submission_review_resubmission_and_private_download_preserve_contract()
    {
        using var app = new ApiFactory();
        app.Prepare();
        SetManager(app);
        using var employee = app.Actor();
        using var manager = app.Actor("Manager", ApiFactory.EmployeeB);
        using var form = ReportForm();
        var response = await employee.PostAsync("/api/EmployeeReport/submit", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = payload.GetProperty("data").GetProperty("id").GetGuid();
        Assert.Equal(1, app.Documents.Count);
        var rows = await manager.GetFromJsonAsync<JsonElement>("/api/EmployeeReport/get-all");
        Assert.Single(rows.GetProperty("data").EnumerateArray());
        var versionId = rows.GetProperty("data")[0].GetProperty("versions")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await employee.GetAsync($"/api/EmployeeReport/download/{versionId}")).StatusCode);
        using var outsider = app.Actor("Employee", ApiFactory.EmployeeB);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/api/EmployeeReport/download/{versionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/api/EmployeeReport/by-employee/{ApiFactory.EmployeeA}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsJsonAsync($"/api/EmployeeReport/review/{id}",
            new { status = EmployeeReportStatus.ChangesRequested, comment = "Please revise." })).StatusCode);
        using var replacement = ReportForm(id);
        Assert.Equal(HttpStatusCode.OK, (await employee.PostAsync("/api/EmployeeReport/submit", replacement)).StatusCode);
        Assert.Equal(1, app.Documents.Count);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        Assert.Single(await db.EmployeeReportVersions.ToListAsync());
        Assert.Equal(EmployeeReportStatus.PendingManager, (await db.EmployeeReports.SingleAsync()).Status);
        Assert.Equal(app.Clock.GetUtcNow().UtcDateTime, (await db.EmployeeReports.SingleAsync()).SubmittedAt);
    }

    [Fact]
    public async Task Failed_report_upload_rolls_back_database_and_removes_files_already_written()
    {
        using var app = new ApiFactory();
        app.Prepare();
        app.Documents.FailOnWrite = 2;
        using var employee = app.Actor();
        using var form = ReportForm();
        form.Add(new ByteArrayContent("second"u8.ToArray()), "Files", "second.pdf");
        Assert.Equal(HttpStatusCode.InternalServerError, (await employee.PostAsync("/api/EmployeeReport/submit", form)).StatusCode);
        Assert.Equal(0, app.Documents.Count);
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<DatabaseContext>().EmployeeReports.ToListAsync());
    }

    [Fact]
    public async Task Handover_files_are_authorized_and_client_stored_names_are_discarded()
    {
        using var app = new ApiFactory();
        app.Prepare();
        SetManager(app);
        using var employee = app.Actor();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("2026-10-17"), "LastWorkingDate");
        form.Add(new StringContent("Personal circumstances"), "Reason");
        form.Add(new StringContent("[{\"projectName\":\"Delivery\",\"progress\":80,\"documents\":[{\"storedName\":\"forged.pdf\"}]}]"), "ProjectsJson");
        form.Add(new StringContent("[{\"projectIndex\":0,\"kind\":\"documents\"},{\"projectIndex\":0,\"kind\":\"handoverFiles\"}]"), "AttachmentMetadataJson");
        form.Add(new ByteArrayContent("document"u8.ToArray()), "Attachments", "project.pdf");
        form.Add(new ByteArrayContent("handover"u8.ToArray()), "Attachments", "handover.zip");
        Assert.Equal(HttpStatusCode.OK, (await employee.PostAsync("/api/EmployeeHandover/request", form)).StatusCode);
        using var scope = app.Services.CreateScope();
        var handover = await scope.ServiceProvider.GetRequiredService<DatabaseContext>().EmployeeHandovers.SingleAsync();
        using var projects = JsonDocument.Parse(handover.ProjectsJson);
        var documents = projects.RootElement[0].GetProperty("documents");
        Assert.Single(documents.EnumerateArray());
        var name = documents[0].GetProperty("storedName").GetString();
        Assert.NotEqual("forged.pdf", name);
        var route = $"/api/EmployeeHandover/{handover.Id}/files/{name}";
        Assert.Equal(HttpStatusCode.OK, (await employee.GetAsync(route)).StatusCode);
        using var manager = app.Actor("Manager", ApiFactory.EmployeeB);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(route)).StatusCode);
        using var outsider = app.Actor("Employee", Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync(route)).StatusCode);
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound,
            (await anonymous.GetAsync($"/uploads/handovers/{handover.Id:N}/{name}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsJsonAsync($"/api/EmployeeHandover/review/{handover.Id}",
            new { approve = true })).StatusCode);
        scope.ServiceProvider.GetRequiredService<DatabaseContext>().ChangeTracker.Clear();
        Assert.Equal(EmployeeStatus.Working,
            (await scope.ServiceProvider.GetRequiredService<DatabaseContext>().Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeA)).Status);
        app.Clock.Now = DateTimeOffset.Parse("2026-10-17T17:00:00Z");
        using var processing = app.Services.CreateScope();
        var processor = processing.ServiceProvider.GetRequiredService<M.Contract.Services.Interface.IEmploymentChangeProcessor>();
        Assert.Equal(1, await processor.ApplyDueAsync());
        Assert.Equal(0, await processor.ApplyDueAsync());
        Assert.Equal(EmployeeStatus.Resigned, (await processing.ServiceProvider.GetRequiredService<DatabaseContext>().Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeA)).Status);
    }

    [Fact]
    public async Task Promotion_uses_vietnam_date_and_manager_scope_and_applies_position_atomically()
    {
        using var app = new ApiFactory();
        app.Prepare();
        SetManager(app);
        var position = new Position { Name = "Senior", Code = "SR" };
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Positions.Add(position);
            await db.SaveChangesAsync();
        }
        using var employee = app.Actor();
        var request = new { newPositionId = position.Id, targetRoleName = "Employee", reason = "Completed responsibilities", effectiveDate = "2026-10-16" };
        Assert.Equal(HttpStatusCode.BadRequest, (await employee.PostAsJsonAsync("/api/EmployeePromotion/request", request)).StatusCode);
        var valid = new { newPositionId = position.Id, targetRoleName = "Employee", reason = "Completed responsibilities", effectiveDate = "2026-10-17" };
        Assert.Equal(HttpStatusCode.OK, (await employee.PostAsJsonAsync("/api/EmployeePromotion/request", valid)).StatusCode);
        using var data = app.Services.CreateScope();
        var dbCheck = data.ServiceProvider.GetRequiredService<DatabaseContext>();
        var promotion = await dbCheck.EmployeePromotions.SingleAsync();
        using var unrelated = app.Actor("Manager", Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unrelated.PostAsJsonAsync($"/api/EmployeePromotion/review/{promotion.Id}", new { approve = true })).StatusCode);
        using var manager = app.Actor("Manager", ApiFactory.EmployeeB, ApiFactory.EmployeeB);
        Assert.Equal(HttpStatusCode.OK,
            (await manager.PostAsJsonAsync($"/api/EmployeePromotion/review/{promotion.Id}", new { approve = true })).StatusCode);
        dbCheck.ChangeTracker.Clear();
        Assert.Equal(position.Id, (await dbCheck.Employees.SingleAsync(e => e.Id == ApiFactory.EmployeeA)).PositionId);
        Assert.Equal(EmployeePromotionStatus.Approved, (await dbCheck.EmployeePromotions.SingleAsync()).Status);
    }

    private static MultipartFormDataContent ReportForm(Guid? reportId = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Monthly report"), "Title");
        form.Add(new StringContent("Operations"), "ReportType");
        form.Add(new StringContent("2026-10"), "Period");
        if (reportId.HasValue) form.Add(new StringContent(reportId.Value.ToString()), "ReportId");
        form.Add(new ByteArrayContent("report"u8.ToArray()), "File", "report.pdf");
        return form;
    }

    private static void SetManager(ApiFactory app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Employees.Single(e => e.Id == ApiFactory.EmployeeA).ManagerId = ApiFactory.EmployeeB;
        db.SaveChanges();
    }
}
