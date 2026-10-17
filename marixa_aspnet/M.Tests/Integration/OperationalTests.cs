using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using M.Tests.Infrastructure;

namespace M.Tests.Integration;

public sealed class OperationalTests
{
    [Fact]
    public async Task Auth_is_rate_limited_with_retry_metadata_and_private_responses_have_no_store()
    {
        using var app = new ApiFactory(authLimit: 5);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "isolated-test-123");
        for (var i = 0; i < 5; i++) await client.PostAsJsonAsync("/api/Auth/login", new { username = "" });
        var result = await client.PostAsJsonAsync("/api/Auth/login", new { username = "" });
        Assert.Equal(HttpStatusCode.TooManyRequests, result.StatusCode);
        Assert.NotNull(result.Headers.RetryAfter);
        Assert.True(result.Headers.CacheControl?.NoStore);
        Assert.Equal("isolated-test-123", result.Headers.GetValues("X-Correlation-ID").Single());
        var error = await result.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("RATE_LIMITED", error.GetProperty("code").GetString());
    }
}
