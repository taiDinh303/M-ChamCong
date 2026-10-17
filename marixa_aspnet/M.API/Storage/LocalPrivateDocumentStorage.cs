using M.Contract.Services.Storage;
using M.Core.Base;
using M.ModelViews.Files;
using Microsoft.AspNetCore.StaticFiles;
using static M.Core.Base.BaseException;

namespace M.API.Storage;

/// <summary>Report and handover documents are never served as public static files.</summary>
public sealed class LocalPrivateDocumentStorage(IWebHostEnvironment environment,
    ILogger<LocalPrivateDocumentStorage> logger) : IPrivateDocumentStorage
{
    private const long MaximumBytes = 20 * 1024 * 1024;

    public async Task SaveAsync(DocumentArea area, Guid? folderId, string storedName, UploadedDocument file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length is <= 0 or > MaximumBytes)
            throw new ErrorException(400, "INVALID_FILE", "Document size must be between 1 byte and 20 MiB.");
        var path = Resolve(area, folderId, storedName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var created = false;
        try
        {
            await using var input = file.OpenReadStream();
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous);
            created = true;
            var buffer = new byte[81920];
            long total = 0;
            int count;
            while ((count = await input.ReadAsync(buffer, cancellationToken)) != 0)
            {
                total += count;
                if (total > file.Length || total > MaximumBytes)
                    throw new ErrorException(400, "INVALID_FILE", "Document length does not match its metadata.");
                await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            }
            if (total != file.Length)
                throw new ErrorException(400, "INVALID_FILE", "Document length does not match its metadata.");
        }
        catch
        {
            if (created) Delete(area, folderId, [storedName]);
            throw;
        }
    }

    public DocumentDownload Open(DocumentArea area, Guid? folderId, string storedName, string fileName)
    {
        var path = Resolve(area, folderId, storedName);
        // Existing handover files remain readable through the authorized endpoint only.
        if (!File.Exists(path) && area == DocumentArea.EmployeeHandovers && folderId.HasValue)
            path = Path.Combine(PhotoStore.GetRoot(environment), "handovers", folderId.Value.ToString("N"), storedName);
        if (!File.Exists(path)) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Document not found.");
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var mime)) mime = "application/octet-stream";
        return new DocumentDownload(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), mime,
            Path.GetFileName(fileName));
    }

    public void Delete(DocumentArea area, Guid? folderId, IEnumerable<string> storedNames)
    {
        foreach (var name in storedNames.Distinct())
        {
            try { File.Delete(Resolve(area, folderId, name)); }
            catch (IOException exception) { logger.LogWarning(exception, "Could not remove an obsolete private document."); }
            catch (UnauthorizedAccessException exception) { logger.LogWarning(exception, "Could not remove an obsolete private document."); }
        }
    }

    private string Resolve(DocumentArea area, Guid? folderId, string storedName)
    {
        if (string.IsNullOrWhiteSpace(storedName) || Path.GetFileName(storedName) != storedName
            || storedName.Contains('\\') || storedName.Contains('/') || storedName is "." or "..")
            throw new ArgumentException("Invalid document name.", nameof(storedName));
        var directory = area switch
        {
            DocumentArea.EmployeeReports => Path.Combine(environment.ContentRootPath, "App_Data", "EmployeeReports"),
            DocumentArea.EmployeeHandovers when folderId.HasValue => Path.Combine(environment.ContentRootPath,
                "App_Data", "EmployeeHandovers", folderId.Value.ToString("N")),
            _ => throw new ArgumentException("Invalid document area or folder.", nameof(area))
        };
        return Path.Combine(directory, storedName);
    }
}
