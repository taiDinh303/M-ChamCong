using M.Core.Base;
using ModelViews.LeaveRequestModelView;

namespace M.Contract.Services.Interface
{
    public interface ILeaveRequestService
    {
        Task<BasePaginatedList<LeaveRequestResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize);

        Task<LeaveRequestResponseModelView> GetByIdAsync(Guid id);

        Task CreateAsync(CreateLeaveRequestModelView model);

        Task UpdateAsync(UpdateLeaveRequestModelView model);

        Task SoftDeleteAsync(Guid id);

        Task DeleteAsync(Guid id);
    }
}