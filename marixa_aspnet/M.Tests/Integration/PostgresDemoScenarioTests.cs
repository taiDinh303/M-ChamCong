using M.API.Configuration;
using M.API.Seed;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using M.Repositories.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace M.Tests.Integration;

public sealed class PostgresDemoScenarioTests
{
    [PostgresFact]
    public async Task Demo_dataset_runs_twice_on_postgres_without_changing_existing_rows()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MARIXA_TEST_POSTGRES"));
        if (connection.Host is not ("127.0.0.1" or "localhost" or "::1")) throw new InvalidOperationException("Loopback only.");
        connection.Pooling = false;
        await using var master = new NpgsqlConnection(connection.ConnectionString); await master.OpenAsync();
        var name = "Marixa_Verify_Demo_" + Guid.NewGuid().ToString("N");
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", master)) await create.ExecuteNonQueryAsync();
        try
        {
            connection.Database = name;
            var services = new ServiceCollection(); services.AddLogging();
            services.AddDbContext<DatabaseContext>(o => o.UseNpgsql(connection.ConnectionString));
            DependencyInjection.AddIdentity(services);
            await using var provider = services.BuildServiceProvider(); await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>(); await db.Database.EnsureCreatedAsync();
            var employee = new Employee { EmployeeCode = "EXISTING", GivenName = "Existing", FamilyName = "Fixture" };
            db.Employees.Add(employee);
            db.AttendanceRules.Add(new AttendanceRule { Code = "FIXTURE", Name = "Fixture", IsActive = true });
            await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            foreach (var role in new[] { "Admin", "Employee" }) Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role })).Succeeded);
            var owner = new ApplicationUser { UserName = "fixture-owner", EmployeeId = employee.Id };
            var password = Guid.NewGuid() + "aA1!";
            Assert.True((await users.CreateAsync(owner, password)).Succeeded); Assert.True((await users.AddToRoleAsync(owner, "Admin")).Succeeded);
            Assert.True(await DemoScenarioSeeder.SeedAsync(db, users, password));
            Assert.False(await DemoScenarioSeeder.SeedAsync(db, users, password));
            Assert.Equal(73, await db.Attendances.CountAsync());
            Assert.Equal(3, await db.EmployeeReports.CountAsync());
            Assert.Equal(10, await db.LeaveLedger.CountAsync());
            Assert.Equal("EXISTING", (await db.Employees.AsNoTracking().SingleAsync(e => e.Id == employee.Id)).EmployeeCode);
            Assert.Null((await db.Employees.AsNoTracking().SingleAsync(e => e.Id == employee.Id)).UserId);
            var approvedOvertime = await db.Attendances.SingleAsync(a => a.Id == DemoScenarioSeeder.Id("attendance-0-9"));
            Assert.Equal(2m, approvedOvertime.OvertimeHours);
            var pendingOvertime = await db.Attendances.SingleAsync(a => a.Id == DemoScenarioSeeder.Id("attendance-0-15"));
            Assert.Equal(0m, pendingOvertime.OvertimeHours);
        }
        finally
        {
            Assert.StartsWith("Marixa_Verify_Demo_", name);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{name}\" WITH (FORCE)", master); await drop.ExecuteNonQueryAsync();
        }
    }
}
