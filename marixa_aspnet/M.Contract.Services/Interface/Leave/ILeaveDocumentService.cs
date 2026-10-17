using M.Contract.Services.Storage;

namespace M.Contract.Services.Interface;

public interface ILeaveDocumentService
{
    Task<DocumentDownload> PdfAsync(Guid id);
}
