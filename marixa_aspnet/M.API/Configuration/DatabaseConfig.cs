using Microsoft.Extensions.Configuration;

namespace M.API.Configuration;

/// <summary>
/// Tập trung mọi việc đọc Database Provider + Connection String.
///
/// Độ ưu tiên (cao → thấp):
///   1. IConfiguration (appsettings.json + appsettings.{Env}.json + env vars đã merge)
///   2. Default: SqlServer (dev local)
///
/// PostgreSQL: tự normalize URI (postgres://user:pass@host/db) về dạng keyword/value
/// (Host=...;Port=...;Database=...;Username=...;Password=...) để Npgsql chấp nhận.
///
/// Dùng ở cả DependencyInjection.AddDatabase (EF setup) và Program.cs (startup migrate/ensure).
/// </summary>
public sealed record DatabaseConfig(
    string Provider,
    string ConnectionString,
    bool IsPostgres)
{
    public static DatabaseConfig FromConfiguration(IConfiguration configuration)
    {
        // Đọc provider (env var DATABASE__PROVIDER được ASP.NET auto-merge vào config)
        string provider =
            configuration["Database:Provider"]
            ?? "SqlServer";

        // Đọc connection string (env var CONNECTIONSTRINGS__MYCNN)
        string connStr =
            configuration.GetConnectionString("MyCnn")
            ?? throw new InvalidOperationException(
                "CONNECTIONSTRINGS__MYCNN is required. Set ConnectionStrings:MyCnn in Development User Secrets " +
                "or the API hosting secret store. DATABASE__PROVIDER must be SqlServer or PostgreSQL");

        if (string.IsNullOrWhiteSpace(connStr) || connStr.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Set the database connection string for this environment.");
        if (!provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) && !provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Database provider must be SqlServer or PostgreSQL.");

        bool isPostgres = provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);

        if (isPostgres)
            connStr = NpgsqlConnectionStringNormalizer.Normalize(connStr);

        return new DatabaseConfig(provider, connStr, isPostgres);
    }
}
