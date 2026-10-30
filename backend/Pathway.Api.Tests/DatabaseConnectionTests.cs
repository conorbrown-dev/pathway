using Npgsql;

using Xunit;

namespace Pathway.Api.Tests;

public class DatabaseConnectionTests
{
    [Fact]
    public void Neon_style_uri_without_port_defaults_to_5432_and_requires_tls()
    {
        var parsed = new NpgsqlConnectionStringBuilder(
            DatabaseConnection.ToNpgsql("postgresql://app_user:s3cr%40t@ep-cool-name-123456.us-east-1.aws.neon.tech/pathway?sslmode=require"));
        Assert.Equal("ep-cool-name-123456.us-east-1.aws.neon.tech", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("pathway", parsed.Database);
        Assert.Equal("app_user", parsed.Username);
        Assert.Equal("s3cr@t", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void Explicit_port_is_kept()
    {
        var parsed = new NpgsqlConnectionStringBuilder(DatabaseConnection.ToNpgsql("postgres://u:p@db.internal:6543/app"));
        Assert.Equal(6543, parsed.Port);
    }

    [Fact]
    public void Npgsql_connection_strings_pass_through_unchanged()
    {
        const string value = "Host=localhost;Database=pathway;Username=dev";
        Assert.Equal(value, DatabaseConnection.ToNpgsql(value));
    }
}
