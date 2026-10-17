using M.Contract.Repositories.Persistence;
using M.Core.Base;
using M.Core.Security;
using static M.Core.Base.BaseException;

namespace M.Services.Services;

/// <summary>Shared actor, clock and repository boundary; no service locator or HTTP state.</summary>
public abstract class BaseService
{
    protected BaseService(IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    protected IUnitOfWork UnitOfWork { get; }
    protected ICurrentUser CurrentUser { get; }
    protected TimeProvider Clock { get; }
    protected string CurrentUserName => CurrentUser.Name;
    protected DateTimeOffset UtcNow => Clock.GetUtcNow();
    protected bool IsStaff => CurrentUser.IsInRole(RoleNames.Admin)
        || CurrentUser.IsInRole(RoleNames.HR) || CurrentUser.IsInRole(RoleNames.Manager);

    protected IGenericRepository<T> Repository<T>() where T : class => UnitOfWork.GetRepository<T>();

    protected void RequireEmployeeAccess(Guid employeeId)
    {
        if (!CurrentUser.IsAuthenticated)
            throw new ErrorException(401, ResponseCodeConstants.UNAUTHORIZED, "Authentication required.");
        if (employeeId == Guid.Empty || (!IsStaff && CurrentUser.EmployeeId != employeeId))
            throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Employee data is outside your access scope.");
    }

    protected Guid RequireCurrentEmployeeId() => CurrentUser.EmployeeId
        ?? throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "An employee profile is required.");

    protected void RequireAdmin()
    {
        if (!CurrentUser.IsInRole(RoleNames.Admin))
            throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Administrator access required.");
    }

    protected void RequireStaff()
    {
        if (!CurrentUser.IsAuthenticated)
            throw new ErrorException(401, ResponseCodeConstants.UNAUTHORIZED, "Authentication required.");
        if (!IsStaff)
            throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Staff access required.");
    }

    protected async Task<T> InTransactionAsync<T>(Func<Task<T>> operation)
    {
        await UnitOfWork.BeginTransactionAsync();
        try
        {
            var result = await operation();
            await UnitOfWork.CommitTransactionAsync();
            return result;
        }
        catch
        {
            await UnitOfWork.RollBackAsync();
            throw;
        }
    }
}
