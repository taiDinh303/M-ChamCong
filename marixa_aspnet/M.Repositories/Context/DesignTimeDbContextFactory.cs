using M.Repositories.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace M.Repositories.Context;

/// <summary>EF tooling creates a context without booting the API or running seeders.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("DATABASE__PROVIDER") ?? "SqlServer";
        var connection = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__MYCNN");
        var options = new DbContextOptionsBuilder<DatabaseContext>();
        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            options.UseNpgsql(PostgresConnectionString.Normalize(connection
                ?? throw new InvalidOperationException("Set CONNECTIONSTRINGS__MYCNN for PostgreSQL tooling.")));
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            options.UseSqlServer(connection ?? "Server=(localdb)\\MSSQLLocalDB;Database=Marixa_001;Integrated Security=True;Encrypt=False");
        else throw new InvalidOperationException("Database provider must be SqlServer or PostgreSQL.");
        return new DatabaseContext(options.Options);
    }
}
