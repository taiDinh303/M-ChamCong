using System.Net;
using M.API.Storage;
using M.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M.Tests.Unit;

public sealed class StorageTests
{
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0];

    [Theory]
    [InlineData("sb_secret_fake-test-key", false)]
    [InlineData("legacy-test-jwt", true)]
    public async Task Supabase_adapter_sends_correct_headers_for_both_key_types(string key, bool bearer)
    {
        var handler = new CaptureHandler();
        using var client = new HttpClient(handler);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Supabase:Url"] = "https://example.supabase.co", ["Supabase:ServiceKey"] = key
        }).Build();
        var storage = new SupabasePhotoStorage(client, config, new FixedClock(DateTimeOffset.Parse("2026-10-31T18:00:00Z")));
        using var image = new MemoryStream(Png);
        var result = await storage.SaveAsync(image, ".png", "checkin");
        Assert.Equal(key, handler.ApiKey);
        Assert.Equal(bearer, handler.HasBearer);
        Assert.Contains("/checkin/202611/", result);
        Assert.Contains("/object/public/marixa-photos/", result);
        Assert.True(image.CanRead);
    }

    [Fact]
    public async Task Empty_canonical_key_preserves_legacy_environment_alias()
    {
        var handler = new CaptureHandler();
        using var client = new HttpClient(handler);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Supabase:Url"] = "https://example.supabase.co", ["Supabase:ServiceKey"] = "",
            ["Supabase:Service_Key"] = "synthetic-legacy-key"
        }).Build();
        var storage = new SupabasePhotoStorage(client, config, TimeProvider.System);
        using var image = new MemoryStream(Png);
        await storage.SaveAsync(image, ".png", "checkin");
        Assert.Equal("synthetic-legacy-key", handler.ApiKey);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("Local", typeof(LocalPhotoStorage))]
    [InlineData("Supabase", typeof(SupabasePhotoStorage))]
    public void Explicit_provider_controls_the_registered_adapter(string provider, Type expected)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = provider, ["Supabase:Url"] = "https://example.supabase.co",
            ["Supabase:ServiceKey"] = "synthetic-key"
        }).Build();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        M.API.Configuration.DependencyInjection.AddPhotoStorage(services, config);
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(new StorageEnvironment());
        using var container = services.BuildServiceProvider();
        using var scope = container.CreateScope();
        Assert.IsType(expected, scope.ServiceProvider.GetRequiredService<M.Contract.Services.Storage.IPhotoStorage>());
    }

    private sealed class StorageEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "StorageTests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public async Task Invalid_image_signature_does_not_reach_network()
    {
        var handler = new CaptureHandler();
        using var client = new HttpClient(handler);
        var storage = new SupabasePhotoStorage(client, new ConfigurationBuilder().Build(), TimeProvider.System);
        using var content = new MemoryStream("not an image"u8.ToArray());
        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(content, ".png", "checkin"));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Oversized_image_is_rejected_before_storage()
    {
        var handler = new CaptureHandler();
        using var client = new HttpClient(handler);
        var storage = new SupabasePhotoStorage(client, new ConfigurationBuilder().Build(), TimeProvider.System);
        using var content = new MemoryStream(new byte[PhotoStorageBase.MaxBytes + 1]);
        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(content, ".png", "checkin"));
        Assert.Equal(0, handler.Calls);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? ApiKey { get; private set; }
        public bool HasBearer { get; private set; }
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            ApiKey = request.Headers.GetValues("apikey").Single();
            HasBearer = request.Headers.Authorization?.Scheme == "Bearer";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
