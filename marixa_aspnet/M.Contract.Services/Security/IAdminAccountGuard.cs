namespace M.Contract.Services.Security;

public interface IAdminAccountGuard
{
    Task ProtectDeactivationAsync(Guid userId, Func<Task> change, CancellationToken cancellationToken = default);
}
