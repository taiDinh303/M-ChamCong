using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Security;
using M.Core.Security;
using Microsoft.EntityFrameworkCore;
using static M.Core.Base.BaseException;

namespace M.Services.Security;

public sealed class AdminAccountGuard(IUnitOfWork unitOfWork, ICurrentUser actor, TimeProvider clock) : IAdminAccountGuard
{
    public async Task EnsureCanManageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!actor.IsAuthenticated || !(actor.IsInRole(RoleNames.Admin) || actor.IsInRole(RoleNames.HR) || actor.IsInRole(RoleNames.Manager)))
            throw new ErrorException(403, "FORBIDDEN", "Staff access required.");
        if (actor.IsInRole(RoleNames.Admin)) return;
        var roles = unitOfWork.GetRepository<ApplicationRole>().Entities;
        var links = unitOfWork.GetRepository<ApplicationUserRole>().Entities;
        if (await links.Where(link => link.UserId == userId)
            .Join(roles.Where(role => role.Name == RoleNames.Admin && !role.DeletedTime.HasValue),
                link => link.RoleId, role => role.Id, (link, role) => link.UserId).AnyAsync(cancellationToken))
            throw new ErrorException(403, "FORBIDDEN", "Only an administrator may manage an administrator account.");
    }

    public async Task ProtectDeactivationAsync(Guid userId, Func<Task> change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var roles = unitOfWork.GetRepository<ApplicationRole>().Entities;
            var links = unitOfWork.GetRepository<ApplicationUserRole>().Entities;
            var adminLinks = links.Join(roles.Where(r => r.Name == RoleNames.Admin && !r.DeletedTime.HasValue),
                link => link.RoleId, role => role.Id, (link, role) => link);
            if (await adminLinks.AnyAsync(link => link.UserId == userId, cancellationToken))
            {
                if (!actor.IsInRole(RoleNames.Admin))
                    throw new ErrorException(403, "FORBIDDEN", "Only an administrator may deactivate an administrator.");
                await EnsureAnotherActiveAdminAsync(userId, cancellationToken);
            }
            await change();
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollBackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task EnsureAnotherActiveAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roles = unitOfWork.GetRepository<ApplicationRole>().Entities;
        var links = unitOfWork.GetRepository<ApplicationUserRole>().Entities;
        var adminIds = links.Join(roles.Where(r => r.Name == RoleNames.Admin && !r.DeletedTime.HasValue),
            link => link.RoleId, role => role.Id, (link, role) => link.UserId);
        if (!await adminIds.ContainsAsync(userId, cancellationToken)) return;
        var now = clock.GetUtcNow();
        if (!await unitOfWork.GetRepository<ApplicationUser>().Entities
                .Where(u => u.Id != userId && !u.DeletedTime.HasValue && (!u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd <= now))
                .AnyAsync(u => adminIds.Contains(u.Id), cancellationToken))
            throw new ErrorException(409, "LAST_ACTIVE_ADMIN", "At least one active administrator must remain.");
    }
}
