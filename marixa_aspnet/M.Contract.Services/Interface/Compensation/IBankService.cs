using M.Core.Base;
using M.ModelViews.BankModelView;

namespace M.Contract.Services.Interface
{
    public interface IBankService
    {
        Task<List<BankResponseModelView>> GetArchivedAsync();

        Task<BasePaginatedList<BankResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize);

        Task<BankResponseModelView> GetByIdAsync(Guid id);

        Task CreateAsync(CreateBankModelView model);

        Task UpdateAsync(UpdateBankModelView model);

        Task SoftDeleteAsync(Guid id);

        Task DeleteAsync(Guid id);
    }
}