using M.Core.Base;
using M.ModelViews.DepartmentModelView;

namespace M.Contract.Services.Interface
{
    public interface IDepartmentService
    {
        Task<BasePaginatedList<DepartmentResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize);

        Task<DepartmentResponseModelView> GetByIdAsync(Guid id);

        Task CreateAsync(CreateDepartmentModelView model);

        Task UpdateAsync(UpdateDepartmentModelView model);

        Task SoftDeleteAsync(Guid id);

        Task DeleteAsync(Guid id);
    }
}