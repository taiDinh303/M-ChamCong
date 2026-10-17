using M.API.Seed;
using M.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace M.API.Bootstrap;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer(IServiceProvider services, IConfiguration configuration,
    IHostEnvironment environment, ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var initialize = configuration.GetValue<bool?>("DatabaseInitialization:Enabled") ?? environment.IsDevelopment();
        var seed = configuration.GetValue<bool?>("Seed:Enabled") ?? environment.IsDevelopment();
        if (!initialize && !seed) return;
        var db = services.GetRequiredService<DatabaseContext>();
        if (initialize)
        {
            if (db.Database.IsNpgsql())
            {
                var statements = db.Database.GenerateCreateScript()
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var statement in statements.Where(s => s.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase)))
                {
                    try { await db.Database.ExecuteSqlRawAsync(statement, cancellationToken); }
                    catch (PostgresException error) when (error.SqlState is "42P07" or "42710" or "42701") { }
                }
                var periodUpgrade = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,
                    "Sql", "20261017-attendance-periods.sql"), cancellationToken);
                await db.Database.ExecuteSqlRawAsync(periodUpgrade, cancellationToken);
                var policyUpgrade = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,
                    "Sql", "20261017-workforce-policies.sql"), cancellationToken);
                await db.Database.ExecuteSqlRawAsync(policyUpgrade, cancellationToken);
                await PostgresSchemaSync.EnsureColumnsAsync(db, services);
                foreach (var statement in statements.Where(s => !s.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase)))
                {
                    try { await db.Database.ExecuteSqlRawAsync(statement, cancellationToken); }
                    catch (PostgresException error) when (error.SqlState is "42P07" or "42710" or "42701") { }
                }
            }
            else await db.Database.MigrateAsync(cancellationToken);
        }
        if (seed)
        {
            await RoleSeeder.SeedAsync(services);
            await AttendanceRuleSeeder.SeedAsync(services);
            if (configuration.GetValue<bool>("Seed:Admin:Enabled")) await AdminSeeder.SeedAsync(services);
            if (configuration.GetValue<bool>("Seed:Sample:Enabled"))
            {
                if (!environment.IsDevelopment())
                    throw new InvalidOperationException("Sample seeding is only allowed in Development.");
                await SampleDataSeeder.SeedAsync(services);
            }
        }
        logger.LogInformation("Explicit database initialization completed for {Environment}", environment.EnvironmentName);
    }
}
