using M.Contract.Services.Storage;
using M.Core.Time;

namespace M.API.Storage;

public abstract class PhotoStorageBase(TimeProvider clock) : IPhotoStorage
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public static bool IsAllowedExtension(string extension) => extension.ToLowerInvariant()
        is ".jpg" or ".jpeg" or ".png" or ".webp";

    public async Task<string> SaveAsync(Stream content, string extension, string kind,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!IsAllowedExtension(extension) || kind is not ("checkin" or "checkout"))
            throw new ArgumentException("Invalid attendance photo extension or kind.");
        using var validated = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (validated.Length + count > MaxBytes) throw new ArgumentException("Photo exceeds 5 MiB.");
            await validated.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
        }
        if (!MatchesSignature(validated.GetBuffer(), (int)validated.Length, extension))
            throw new ArgumentException("Photo content does not match its image extension.");
        validated.Position = 0;
        var month = BusinessTime.DateAt(clock.GetUtcNow()).ToString("yyyyMM");
        var objectPath = $"{kind}/{month}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        return await StoreAsync(validated, objectPath, cancellationToken);
    }

    private static bool MatchesSignature(byte[] image, int length, string extension)
    {
        var bytes = image.AsSpan(0, length);
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
            ".png" => bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    protected abstract Task<string> StoreAsync(Stream content, string objectPath,
        CancellationToken cancellationToken);
}
