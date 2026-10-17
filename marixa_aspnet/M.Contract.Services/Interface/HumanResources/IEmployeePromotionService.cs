using M.ModelViews.HumanResources;
using M.ModelViews.Files;
using M.Contract.Services.Storage;

namespace M.Contract.Services.Interface;

public interface IEmployeePromotionService
{
    Task<object> Mine();
    Task<object> GetAll();
    Task<object> Options();
    Task<object> CreateRequest(PromotionRequest form);
    Task<object> Review(Guid id, PromotionReview form);
}
