using Microsoft.Extensions.Configuration;

namespace M.API;

/// <summary>
/// Tập trung mọi việc đọc Database Provider + Connection String.
///
/// Độ ưu tiên (cao → thấp):
///   1. IConfiguration (appsettings.json + appsettings.{Env}.json + env vars đã merge)
///   2. Environment.GetEnvironmentVariable (fallback trực tiếp)
///   3. Default: SqlServer (dev local)
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
            ?? Environment.GetEnvironmentVariable("DATABASE__PROVIDER")
            ?? Environment.GetEnvironmentVariable("Database__Provider")
            ?? "SqlServer";

        // Đọc connection string (env var CONNECTIONSTRINGS__MYCNN)
        string connStr =
            configuration.GetConnectionString("MyCnn")
            ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__MYCNN")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__MyCnn")
            ?? throw new InvalidOperationException(
                "Database connection string 'MyCnn' not found.\n" +
                "  Dev local (SQL Server):  đặt ConnectionStrings:MyCnn trong appsettings.json\n" +
                "  Prod  (PostgreSQL):      đặt env var CONNECTIONSTRINGS__MYCNN trong Render/Vercel/Supabase\n" +
                "    Example: postgresql://user:pass@host:5432/marixa?sslmode=require\n" +
                "  Đồng thời đặt DATABASE__PROVIDER = SqlServer | PostgreSQL");

        bool isPostgres = provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);

        if (isPostgres)
            connStr = NpgsqlConnectionStringNormalizer.Normalize(connStr);

        return new DatabaseConfig(provider, connStr, isPostgres);
    }
}
