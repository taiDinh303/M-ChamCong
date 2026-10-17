using M.Contract.Services.Storage;
using M.ModelViews.Files;

namespace M.Tests.Infrastructure;

public sealed class MemoryDocuments : IPrivateDocumentStorage
{
    private readonly Dictionary<(DocumentArea, Guid?, string), byte[]> _files = [];
    public int Count => _files.Count;
    public int? FailOnWrite { get; set; }
    private int _writes;

    public async Task SaveAsync(DocumentArea area, Guid? folderId, string storedName, UploadedDocument file,
        CancellationToken cancellationToken = default)
    {
        if (++_writes == FailOnWrite) throw new IOException("Simulated isolated storage failure.");
        await using var input = file.OpenReadStream();
        using var output = new MemoryStream();
        await input.CopyToAsync(output, cancellationToken);
        _files.Add((area, folderId, storedName), output.ToArray());
    }

    public DocumentDownload Open(DocumentArea area, Guid? folderId, string storedName, string fileName) =>
        new(new MemoryStream(_files[(area, folderId, storedName)]), "application/octet-stream", fileName);

    public void Delete(DocumentArea area, Guid? folderId, IEnumerable<string> storedNames)
    {
        foreach (var name in storedNames) _files.Remove((area, folderId, name));
    }
}
