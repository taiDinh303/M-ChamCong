using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Security;
using M.Core.Security;
using Microsoft.EntityFrameworkCore;
using static M.Core.Base.BaseException;

namespace M.Services.Security;

public sealed class AdminAccountGuard(IUnitOfWork unitOfWork, ICurrentUser actor, TimeProvider clock) : IAdminAccountGuard
{
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
                var now = clock.GetUtcNow();
                var users = unitOfWork.GetRepository<ApplicationUser>().Entities;
                var anotherAdmin = await users.Where(u => u.Id != userId && !u.DeletedTime.HasValue
                        && (!u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd <= now))
                    .Join(adminLinks, user => user.Id, link => link.UserId, (user, link) => user.Id)
                    .AnyAsync(cancellationToken);
                if (!anotherAdmin)
                    throw new ErrorException(409, "LAST_ACTIVE_ADMIN", "At least one active administrator must remain.");
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
}
