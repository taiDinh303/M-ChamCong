using M.Contract.Repositories.Entities;

namespace M.Contract.Services.Interface;

public interface IEmploymentChangeProcessor
{
    // Review services call these inside their existing transaction; no HTTP endpoint exposes them.
    Task ApplyPromotionIfDueAsync(EmployeePromotion promotion);
    Task ApplyHandoverIfDueAsync(EmployeeHandover handover);
    Task<int> ApplyDueAsync(CancellationToken cancellationToken = default);
}
