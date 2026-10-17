using M.Contract.Services.Storage;
using M.ModelViews.Files;

namespace M.Services.Services;

/// <summary>Compensates newly written files when the surrounding DB transaction fails.</summary>
internal sealed class PrivateDocumentWriteBatch(IPrivateDocumentStorage storage) : IDisposable
{
    private readonly List<(DocumentArea Area, Guid? FolderId, string Name)> _created = [];
    private readonly List<(DocumentArea Area, Guid? FolderId, string Name)> _obsolete = [];
    private bool _committed;

    public async Task SaveAsync(DocumentArea area, Guid? folderId, string name, UploadedDocument file)
    {
        await storage.SaveAsync(area, folderId, name, file);
        _created.Add((area, folderId, name));
    }

    public void DeleteAfterCommit(DocumentArea area, Guid? folderId, IEnumerable<string> names) =>
        _obsolete.AddRange(names.Select(name => (area, folderId, name)));

    public void Complete()
    {
        _committed = true;
        Delete(_obsolete);
    }

    public void Dispose()
    {
        if (!_committed) Delete(_created);
    }

    private void Delete(IEnumerable<(DocumentArea Area, Guid? FolderId, string Name)> files)
    {
        foreach (var group in files.GroupBy(file => (file.Area, file.FolderId)))
            storage.Delete(group.Key.Area, group.Key.FolderId, group.Select(file => file.Name));
    }
}
