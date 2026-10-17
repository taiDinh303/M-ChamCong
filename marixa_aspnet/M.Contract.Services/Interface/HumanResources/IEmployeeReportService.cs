using M.ModelViews.HumanResources;
using M.ModelViews.Files;
using M.Contract.Services.Storage;

namespace M.Contract.Services.Interface;

public interface IEmployeeReportService
{
    Task<object> GetAll(string? search, string? period,
        Guid? departmentId, Guid? employeeId, int? status,
        string? reportType, bool? overdue);
    Task<object> Mine();
    Task<object> ByEmployee(Guid employeeId);
    Task<object> Submit(ReportSubmission form);
    Task<object> Review(Guid id, ReviewForm form);
    Task<object> Forward(Guid id, ForwardForm form);
    Task<object> UpperRequest(Guid id, UpperRequestForm form);
    Task<object> UpperDecision(Guid id, UpperRequestForm form);
    Task<object> Respond(Guid id, RespondForm form);
    Task<object> Complete(Guid id);
    Task<object> Viewed(Guid id);
    Task<object> UploadForUpper(Guid id, UploadedDocument? file, string? note, string? recipient);
    Task<object> MarkSent(Guid id);
    Task<DocumentDownload> Download(Guid versionId);
    Task<DocumentDownload> DownloadAttachment(Guid attachmentId);
    Task<DocumentDownload> DownloadUpper(Guid id);
}
