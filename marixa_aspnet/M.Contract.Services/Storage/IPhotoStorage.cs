namespace M.Contract.Services.Storage;

/// <summary>Stores attendance evidence without exposing a filesystem or cloud SDK to callers.</summary>
public interface IPhotoStorage
{
    Task<string> SaveAsync(Stream content, string extension, string kind,
        CancellationToken cancellationToken = default);
}
