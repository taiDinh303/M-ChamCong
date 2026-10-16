using Npgsql;

namespace M.Repositories.Configuration;

public static class PostgresConnectionString
{
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var value = connectionString.Trim();
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return new NpgsqlConnectionStringBuilder(value).ConnectionString;

        var uri = new Uri(value, UriKind.Absolute);
        if (string.IsNullOrWhiteSpace(uri.Host)) throw new FormatException("PostgreSQL host is required.");
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host, Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'))
        };
        var credentials = uri.UserInfo.Split(':', 2);
        builder.Username = Uri.UnescapeDataString(credentials[0]);
        if (credentials.Length == 2) builder.Password = Uri.UnescapeDataString(credentials[1]);

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2) throw new FormatException("Invalid PostgreSQL URI option.");
            var name = parts[0].ToLowerInvariant() switch
            {
                "sslmode" => "SSL Mode",
                "sslrootcert" => "Root Certificate",
                "connect_timeout" => "Timeout",
                "command_timeout" => "Command Timeout",
                "application_name" => "Application Name",
                "pooling" => "Pooling",
                _ => throw new FormatException("Unsupported PostgreSQL URI option; use keyword/value configuration.")
            };
            builder[name] = Uri.UnescapeDataString(parts[1]);
        }
        return builder.ConnectionString;
    }
}
