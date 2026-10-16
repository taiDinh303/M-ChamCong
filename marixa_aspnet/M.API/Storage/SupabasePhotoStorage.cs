using System.Net.Http.Headers;

namespace M.API.Storage;

/// <summary>Public Storage adapter. Secret API keys and legacy JWT keys use their respective headers.</summary>
public sealed class SupabasePhotoStorage(HttpClient client, IConfiguration configuration, TimeProvider clock)
    : PhotoStorageBase(clock)
{
    public const string Bucket = "marixa-photos";

    public static string? ServiceKey(IConfiguration configuration) =>
        configuration["Supabase:ServiceKey"] ?? configuration["Supabase:Service_Key"];

    public static bool IsConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Supabase:Url"])
        && !string.IsNullOrWhiteSpace(ServiceKey(configuration));

    protected override async Task<string> StoreAsync(Stream content, string objectPath,
        CancellationToken cancellationToken)
    {
        var url = configuration["Supabase:Url"]?.TrimEnd('/');
        var key = ServiceKey(configuration);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Valid HTTPS Storage URL and server credential are required.");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{url}/storage/v1/object/{Bucket}/{objectPath}");
        request.Headers.Add("apikey", key);
        if (!key.StartsWith("sb_secret_", StringComparison.Ordinal))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        // The caller owns its input stream; do not let HttpContent dispose it.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > MaxBytes) throw new ArgumentException("Photo exceeds 5 MiB.");
        request.Content = new ByteArrayContent(buffer.ToArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Storage rejected the upload.", null, response.StatusCode);
        return $"{url}/storage/v1/object/public/{Bucket}/{objectPath}";
    }
}
