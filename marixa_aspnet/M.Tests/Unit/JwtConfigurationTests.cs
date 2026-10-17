using System.Security.Cryptography;
using M.API.Configuration;
using M.Core.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace M.Tests.Unit;

public sealed class JwtConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("                                ")]
    [InlineData("a-private-but-short-key")]
    [InlineData("CHANGE_ME_IN_ENV_VAR_JWTSETTINGS__KEY")]
    [InlineData("LOCAL-DEVELOPMENT-ONLY-CHANGE-ME-32-BYTES")]
    public async Task Production_startup_rejects_invalid_key_with_actionable_error_and_no_secret(string? key)
    {
        using var host = CreateHost(key);

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains("JWTSETTINGS__KEY", error.Message);
        if (!string.IsNullOrWhiteSpace(key)) Assert.DoesNotContain(key, error.Message);
    }

    [Fact]
    public async Task Production_startup_reads_double_underscore_environment_key_and_uses_it_for_bearer_validation()
    {
        // A unique prefix isolates this fixture from all real process credentials and parallel tests.
        var prefix = $"MARIXA_JWT_TEST_{Guid.NewGuid():N}_";
        var variable = prefix + "JWTSETTINGS__KEY";
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Environment.SetEnvironmentVariable(variable, key);
        try
        {
            using var host = CreateHost("CHANGE_ME_IN_ENV_VAR_JWTSETTINGS__KEY", prefix);
            await host.StartAsync();

            var settings = host.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
            var bearer = host.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);
            Assert.Equal(key, settings.Key);
            Assert.Equal(System.Text.Encoding.UTF8.GetBytes(key),
                Assert.IsType<SymmetricSecurityKey>(bearer.TokenValidationParameters.IssuerSigningKey).Key);
            Assert.True(bearer.TokenValidationParameters.ValidateIssuerSigningKey);
            Assert.True(bearer.TokenValidationParameters.ValidateLifetime);

            await host.StopAsync();
        }
        finally { Environment.SetEnvironmentVariable(variable, null); }
    }

    private static IHost CreateHost(string? key, string? environmentPrefix = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = key,
            ["JwtSettings:Issuer"] = "API",
            ["JwtSettings:Audience"] = "APIUser"
        });
        if (environmentPrefix is not null) builder.Configuration.AddEnvironmentVariables(environmentPrefix);
        builder.Services.AddJwtAuthentication(builder.Configuration, isProduction: true);
        return builder.Build();
    }
}
