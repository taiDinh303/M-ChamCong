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
    [InlineData(false, 60)]
    [InlineData(true, 10080)]
    public async Task Login_remember_me_and_response_expiry_match_the_signed_token(bool remember, int minutes)
    {
        using var app = new ApiFactory(); app.Prepare();
        var password = Guid.NewGuid() + "Aa1!";
        using (var scope = app.Services.CreateScope())
            Assert.True((await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
                .CreateAsync(new ApplicationUser { UserName = "expiry-test" }, password)).Succeeded);
        using var client = app.CreateClient();
        var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client,
            "/api/Auth/login", new { username = "expiry-test", password, rememberMe = remember });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(data.GetProperty("token").GetString());
        Assert.Equal(app.Clock.GetUtcNow().UtcDateTime.AddMinutes(minutes), jwt.ValidTo);
        Assert.Equal(jwt.ValidTo, data.GetProperty("expiredAt").GetDateTime().ToUniversalTime());
    }

    [Fact]
    public async Task Failed_password_attempts_lock_the_account_without_issuing_a_cookie()
    {
        using var app = new ApiFactory();
        app.Prepare();
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.CreateAsync(new ApplicationUser
            {
                UserName = "lockout-test", LockoutEnabled = true
            }, Guid.NewGuid() + "Aa1!")).Succeeded);
        }
        using var client = app.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client,
                "/api/Auth/login", new { username = "lockout-test", password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }
        using var check = app.Services.CreateScope();
        var manager = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await manager.IsLockedOutAsync((await manager.FindByNameAsync("lockout-test"))!));
    }

    [Theory]
    [InlineData("block")]
    [InlineData("stamp")]
    [InlineData("role")]
    [InlineData("employee-link")]
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
            employee!.UserId = user.Id;
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveAsync();
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
            if (change == "employee-link")
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                (await uow.GetRepository<Employee>().GetByIdAsync(ApiFactory.EmployeeA))!.UserId = null;
                await uow.SaveAsync();
            }
            var changed = change switch
            {
                "block" => await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddDays(1)),
                "stamp" => await users.UpdateSecurityStampAsync(user!),
                "employee-link" => IdentityResult.Success,
                _ => await users.RemoveFromRoleAsync(user!, "Employee")
            };
            Assert.True(changed.Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Attendance/by-employee/" + ApiFactory.EmployeeA)).StatusCode);
    }
}
