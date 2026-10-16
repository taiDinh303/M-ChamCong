using M.Repositories.Configuration;

namespace M.API.Configuration;

public static class NpgsqlConnectionStringNormalizer
{
    public static string Normalize(string connectionString) => PostgresConnectionString.Normalize(connectionString);
}
