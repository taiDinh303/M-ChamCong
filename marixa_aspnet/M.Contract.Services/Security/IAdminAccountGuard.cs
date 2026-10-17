namespace M.Contract.Services.Security;

public interface IAdminAccountGuard
{
    Task EnsureCanManageAsync(Guid userId, CancellationToken cancellationToken = default);
    Task EnsureAnotherActiveAdminAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ProtectDeactivationAsync(Guid userId, Func<Task> change, CancellationToken cancellationToken = default);
}
