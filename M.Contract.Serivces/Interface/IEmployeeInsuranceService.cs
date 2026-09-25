using M.Core.Base;
using ModelViews.EmployeeInsuranceModelView;

namespace M.Contract.Services.Interface
{
    public interface IEmployeeInsuranceService
    {
        Task<BasePaginatedList<EmployeeInsuranceResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize);

        Task<EmployeeInsuranceResponseModelView> GetByIdAsync(Guid id);

        Task CreateAsync(CreateEmployeeInsuranceModelView model);

        Task UpdateAsync(UpdateEmployeeInsuranceModelView model);

        Task SoftDeleteAsync(Guid id);

        Task DeleteAsync(Guid id);
    }
}