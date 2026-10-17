using M.ModelViews.HumanResources;
using M.ModelViews.Files;
using M.Contract.Services.Storage;

namespace M.Contract.Services.Interface;

public interface IEmployeeHandoverService
{
    Task<object> Mine();
    Task<object> GetAll();
    Task<object> SubmitRequest(HandoverSubmission form);
    Task<DocumentDownload> DownloadFile(Guid id, string storedName);
    Task<object> Review(Guid id, HandoverReview form);
}
