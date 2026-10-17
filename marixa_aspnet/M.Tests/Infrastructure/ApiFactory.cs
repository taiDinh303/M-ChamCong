using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using M.Contract.Repositories.Entities;
using M.Contract.Services.Storage;
using M.Repositories.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace M.Tests.Infrastructure;

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class ApiFactory(bool realJwt = false, int authLimit = 20) : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public static readonly Guid EmployeeA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid EmployeeB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public FixedClock Clock { get; } = new(realJwt ? DateTimeOffset.UtcNow : DateTimeOffset.Parse("2026-10-16T18:05:00Z"));
    public string? AuthenticationFailure { get; private set; }
    public MemoryDocuments Documents { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DatabaseInitialization:Enabled"] = "false", ["Seed:Enabled"] = "false",
            ["RateLimiting:AuthRequestsPerMinute"] = authLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Database:Provider"] = "SqlServer", ["ConnectionStrings:MyCnn"] = "Server=(localdb)\\MSSQLLocalDB;Database=UnusedTestPlaceholder",
            ["JwtSettings:Key"] = "TEST-HOST-ONLY-NOT-A-REAL-SIGNING-KEY-123456",
            ["Supabase:Url"] = "", ["Supabase:ServiceKey"] = "", ["Supabase:Service_Key"] = ""
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<DatabaseContext>>();
            services.RemoveAll<DatabaseContext>();
            services.AddScoped<DatabaseContext>(sp => new TestDatabaseContext(
                new DbContextOptionsBuilder<DatabaseContext>().UseSqlite(_connection).Options,
                sp.GetRequiredService<M.Core.Security.ICurrentUser>(), sp.GetRequiredService<TimeProvider>()));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<IPrivateDocumentStorage>();
            services.AddSingleton<IPrivateDocumentStorage>(Documents);
            if (realJwt) services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>("Bearer", options =>
            {
                var previous = options.Events.OnChallenge;
                options.Events.OnChallenge = async context =>
                {
                    AuthenticationFailure = context.AuthenticateFailure?.GetType().Name;
                    await previous(context);
                };
            });
            if (!realJwt) services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        });
    }

    public void Prepare()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Database.EnsureCreated();
        db.Employees.AddRange(NewEmployee(EmployeeA, "A"), NewEmployee(EmployeeB, "B"));
        db.AttendanceRules.Add(new AttendanceRule
        {
            Code = "TEST", Name = "Test rule", CheckInTime = new TimeOnly(8, 0),
            CheckOutTime = new TimeOnly(17, 0), StandardHours = 8, BreakMinutes = 60, IsActive = true
        });
        db.SaveChanges();
    }

    public HttpClient Actor(string role = "Employee", Guid? employee = null, Guid? user = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-Employee", (employee ?? EmployeeA).ToString());
        client.DefaultRequestHeaders.Add("X-Test-User", (user ?? UserA).ToString());
        return client;
    }

    private static Employee NewEmployee(Guid id, string code) => new()
    {
        Id = id, EmployeeCode = "TEST-" + code, GivenName = code, FamilyName = "Test",
        Status = EmployeeStatus.Working, StartDate = new DateTime(2026, 1, 1)
    };

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

internal sealed class TestDatabaseContext(DbContextOptions<DatabaseContext> options, M.Core.Security.ICurrentUser actor,
    TimeProvider clock) : DatabaseContext(options, actor, clock)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // SQLite is an isolated relational test double. Production provider behavior is tested separately.
        foreach (var property in builder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
        {
            property.SetColumnType(null);
            if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
        }
    }
}

internal sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Request.Headers["X-Test-User"].FirstOrDefault() ?? ApiFactory.UserA.ToString()),
            new Claim(ClaimTypes.Name, "test-actor"), new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("employeeId", Request.Headers["X-Test-Employee"].ToString())
        };
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return Task.CompletedTask;
    }
}
