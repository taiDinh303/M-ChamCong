using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace M.API.Configuration;

public static class RateLimitConfig
{
    public sealed class AuthRateOptions { public int AuthRequestsPerMinute { get; set; } = 20; }
    public static void AddAuthRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthRateOptions>().Bind(configuration.GetSection("RateLimiting"))
            .Validate(o => o.AuthRequestsPerMinute is >= 5 and <= 1000, "AuthRequestsPerMinute must be between 5 and 1000.")
            .ValidateOnStart();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = context.RequestServices.GetRequiredService<IOptions<AuthRateOptions>>().Value.AuthRequestsPerMinute,
                    Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
                }));
            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    statusCode = 429, code = "RATE_LIMITED", message = "Too many authentication requests. Try again later.",
                    data = (object?)null, traceId = context.HttpContext.TraceIdentifier
                }, token);
            };
        });
    }
}
