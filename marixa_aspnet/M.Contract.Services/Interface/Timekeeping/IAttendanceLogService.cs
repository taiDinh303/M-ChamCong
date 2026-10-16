using M.Core.Base;
using M.ModelViews.AttendanceLogModelView;

namespace M.Contract.Services.Interface
{
    public interface IAttendanceLogService
    {
        Task<BasePaginatedList<AttendanceLogResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize);

        Task<AttendanceLogResponseModelView> GetByIdAsync(Guid id);

        Task CreateAsync(CreateAttendanceLogModelView model);

        Task UpdateAsync(UpdateAttendanceLogModelView model);

        Task AdjustAsync(AdjustAttendanceLogModelView model);

        Task SoftDeleteAsync(Guid id);

        Task DeleteAsync(Guid id);
    }
}
