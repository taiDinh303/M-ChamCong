using System.Net;
using System.Text;
using M.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Integration;

public sealed class ContractInventoryTests
{
    [Fact]
    public void Only_documented_endpoints_are_anonymous_and_the_api_inventory_matches_runtime_routes()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var endpoints = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true
                || e.RoutePattern.RawText?.StartsWith("health/", StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(e => e.RoutePattern.RawText).ToList();
        Assert.True(endpoints.Count > 100);
        var output = new StringBuilder("# Danh mục API từ runtime\n\n> Sinh từ EndpointDataSource/TestServer; không phải bằng chứng API live. Fallback policy yêu cầu đăng nhập. Role attributes không thay owner/service guards; xem16-security-privacy.md.\n\n| Method | Route | Role khai báo | Anonymous |\n| --- | --- | --- | --- |\n");
        foreach (var endpoint in endpoints)
        {
            var route = endpoint.RoutePattern.RawText!;
            var anonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null;
            if (anonymous) Assert.Contains(route.ToLowerInvariant(), new[] { "api/auth/login", "api/auth/activate", "health/live", "health/ready" });
            var roles = string.Join(" AND ", endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Roles).Where(r => !string.IsNullOrWhiteSpace(r)));
            var methods = string.Join(", ", endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []);
            output.AppendLine($"| {methods} | /{route} | {(roles.Length == 0 ? "Authenticated/fallback" : roles)} | {(anonymous ? "YES" : "NO")} |");
        }
        var directory = Environment.GetEnvironmentVariable("MARIXA_ARTIFACT_DIR");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "api-inventory.md"), output.ToString());
        }
    }
}
