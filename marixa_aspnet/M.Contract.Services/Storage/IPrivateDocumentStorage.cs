using M.ModelViews.Files;

namespace M.Contract.Services.Storage;

public enum DocumentArea { EmployeeReports, EmployeeHandovers }

public sealed record DocumentDownload(Stream Content, string ContentType, string FileName);

/// <summary>Private documents must only be opened after the business service authorizes access.</summary>
public interface IPrivateDocumentStorage
{
    Task SaveAsync(DocumentArea area, Guid? folderId, string storedName, UploadedDocument file,
        CancellationToken cancellationToken = default);
    DocumentDownload Open(DocumentArea area, Guid? folderId, string storedName, string fileName);
    void Delete(DocumentArea area, Guid? folderId, IEnumerable<string> storedNames);
}
