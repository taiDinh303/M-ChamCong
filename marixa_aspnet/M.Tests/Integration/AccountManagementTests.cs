using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using M.Contract.Repositories.AuthEntities;
using M.ModelViews.UserModelView;
using M.Repositories.Context;
using M.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class AccountManagementTests
{
    [Theory]
    [InlineData("HR")]
    [InlineData("Manager")]
    public async Task Staff_cannot_create_administrator_accounts(string role)
    {
        using var app = new ApiFactory();
        app.Prepare();
        using var staff = app.Actor(role);
        var response = await staff.PostAsJsonAsync("/api/User/create", new CreateUserModelView
        {
            Username = "new-admin", Email = "new@example.test", Password = "Test-only_Strong42!", Role = "Admin"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<DatabaseContext>().Users.ToListAsync());
    }

    [Fact]
    public async Task System_admin_role_cannot_be_renamed()
    {
        using var app = new ApiFactory();
        app.Prepare();
        var role = new ApplicationRole { Name = "Admin", NormalizedName = "ADMIN" };
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }
        using var admin = app.Actor("Admin");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync("/api/Role/update", new { id = role.Id, name = "Owner" })).StatusCode);
        using var check = app.Services.CreateScope();
        Assert.Equal("Admin", (await check.ServiceProvider.GetRequiredService<DatabaseContext>().Roles.SingleAsync()).Name);
    }

    [Fact]
    public async Task Staff_cannot_issue_or_read_activation_codes_for_admins()
    {
        using var app = new ApiFactory();
        app.Prepare();
        var user = new ApplicationUser { UserName = "admin-test", NormalizedUserName = "ADMIN-TEST", SecurityStamp = Guid.NewGuid().ToString() };
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var role = new ApplicationRole { Name = "Admin", NormalizedName = "ADMIN" };
            db.Users.Add(user);
            db.Roles.Add(role);
            db.UserRoles.Add(new ApplicationUserRole { UserId = user.Id, RoleId = role.Id });
            db.Employees.Single(e => e.Id == ApiFactory.EmployeeB).UserId = user.Id;
            // Legacy records may not have populated ActivationCode.UserId.
            db.ActivationCodes.Add(new ActivationCode { EmployeeId = ApiFactory.EmployeeB, Code = "ISOLATED-TEST-CODE", ExpiresAt = app.Clock.GetUtcNow().UtcDateTime.AddDays(1) });
            await db.SaveChangesAsync();
        }
        using var staff = app.Actor("HR");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await staff.PostAsJsonAsync("/api/Auth/create-activation-code", new { employeeId = ApiFactory.EmployeeB, code = "NEW-TEST-CODE" })).StatusCode);
        var list = await staff.GetFromJsonAsync<JsonElement>("/api/ActivationCode/get-all");
        Assert.Equal(0, list.GetProperty("data").GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task Account_update_persists_and_normalizes_email()
    {
        using var app = new ApiFactory();
        app.Prepare();
        var user = new ApplicationUser { UserName = "employee-test", NormalizedUserName = "EMPLOYEE-TEST", SecurityStamp = Guid.NewGuid().ToString() };
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        using var staff = app.Actor("HR");
        Assert.Equal(HttpStatusCode.OK, (await staff.PutAsJsonAsync("/api/User/update",
            new { id = user.Id, email = "new@example.test", phoneNumber = "+84901234567" })).StatusCode);
        using var check = app.Services.CreateScope();
        var result = await check.ServiceProvider.GetRequiredService<DatabaseContext>().Users.SingleAsync();
        Assert.Equal("new@example.test", result.Email);
        Assert.Equal("NEW@EXAMPLE.TEST", result.NormalizedEmail);
        Assert.Equal("+84901234567", result.PhoneNumber);
    }

    [Fact]
    public async Task Employee_creation_failure_rolls_back_the_identity_account()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Roles.Add(new ApplicationRole { Name = "Employee", NormalizedName = "EMPLOYEE" });
            await db.SaveChangesAsync();
        }
        using var staff = app.Actor("HR");
        var response = await staff.PostAsJsonAsync("/api/User/create", new CreateUserModelView
        {
            Username = "orphan-test", Email = "orphan@example.test", Password = "Test-only_Strong42!", Role = "Employee",
            CreateEmployeeModelView = new() { EmployeeCode = "ORPHAN-TEST", GivenName = "Test", FamilyName = "Case", DepartmentId = Guid.NewGuid() }
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var check = app.Services.CreateScope();
        Assert.Empty(await check.ServiceProvider.GetRequiredService<DatabaseContext>().Users.ToListAsync());
    }
}
