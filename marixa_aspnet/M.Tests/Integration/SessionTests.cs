using System.Net;
using System.Net.Http.Headers;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Repositories.Entities;
using M.Services.Security;
using M.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class SessionTests
{
    [Theory]
    [InlineData("block")]
    [InlineData("stamp")]
    [InlineData("role")]
    public async Task Existing_JWT_is_rejected_after_security_state_changes(string change)
    {
        using var app = new ApiFactory(realJwt: true);
        app.Prepare();
        string token;
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            Assert.True((await roles.CreateAsync(new ApplicationRole { Name = "Employee" })).Succeeded);
            var user = new ApplicationUser { Id = ApiFactory.UserA, UserName = "jwt-test", LockoutEnabled = true };
            Assert.True((await users.CreateAsync(user, Guid.NewGuid() + "Aa1!")).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, "Employee")).Succeeded);
            var employee = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Employee>().GetByIdAsync(ApiFactory.EmployeeA);
            token = await scope.ServiceProvider.GetRequiredService<JwtGenerator>().GenerateAsync(user, employee);
        }
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var initial = await client.GetAsync("/api/Attendance/by-employee/" + ApiFactory.EmployeeA);
        Assert.True(initial.StatusCode == HttpStatusCode.OK, app.AuthenticationFailure ?? initial.StatusCode.ToString());
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(ApiFactory.UserA.ToString());
            var changed = change switch
            {
                "block" => await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddDays(1)),
                "stamp" => await users.UpdateSecurityStampAsync(user!),
                _ => await users.RemoveFromRoleAsync(user!, "Employee")
            };
            Assert.True(changed.Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Attendance/by-employee/" + ApiFactory.EmployeeA)).StatusCode);
    }
}
