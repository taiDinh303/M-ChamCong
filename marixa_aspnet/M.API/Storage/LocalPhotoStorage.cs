namespace M.API.Storage;

public sealed class LocalPhotoStorage(IWebHostEnvironment environment, TimeProvider clock)
    : PhotoStorageBase(clock)
{
    protected override async Task<string> StoreAsync(Stream content, string objectPath,
        CancellationToken cancellationToken)
    {
        var root = PhotoStore.GetRoot(environment);
        var path = Path.Combine(root, "attendance", objectPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous);
            await content.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            File.Delete(path);
            throw;
        }
        return $"/uploads/attendance/{objectPath}";
    }
}
