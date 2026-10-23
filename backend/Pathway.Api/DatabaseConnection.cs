using Npgsql;

namespace Pathway.Api;

public static class DatabaseConnection
{
    private const int DefaultPostgresPort = 5432;

    /// <summary>
    /// Accepts either an Npgsql connection string or a postgres:// / postgresql:// URI (the form
    /// Railway, Neon, Fly and most hosts provide) and returns an Npgsql connection string that
    /// always requires TLS. URIs without an explicit port (Neon's default) use 5432; System.Uri
    /// reports -1 for them because it has no default port for these schemes.
    /// </summary>
    public static string ToNpgsql(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
            return value;

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPostgresPort,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        };
        return builder.ConnectionString;
    }
}
