using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Contract.Services.Security;
using M.Core.Security;
using M.Core.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static M.Core.Base.BaseException;

namespace M.Services.Services.HumanResources;

public sealed class EmploymentChangeProcessor(IUnitOfWork uow, ICurrentUser actor, TimeProvider clock,
    UserManager<ApplicationUser> users, IAdminAccountGuard adminGuard, ILogger<EmploymentChangeProcessor> logger)
    : BaseService(uow, actor, clock), IEmploymentChangeProcessor
{
    private static readonly string[] AssignableRoles = ["Employee", "Manager", "HR"];
    private DateTime Today => DateTime.SpecifyKind(BusinessTime.DateAt(UtcNow).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    public async Task ApplyPromotionIfDueAsync(EmployeePromotion promotion)
    {
        if (promotion.Status != EmployeePromotionStatus.Approved || promotion.AppliedAt.HasValue || promotion.EffectiveDate.Date > Today) return;
        var employee = await Repository<Employee>().Entities.SingleOrDefaultAsync(e => e.Id == promotion.EmployeeId && !e.DeletedTime.HasValue)
            ?? throw new ErrorException(409, "EMPLOYEE_UNAVAILABLE", "The approved employee profile is unavailable.");
        if (employee.Status is EmployeeStatus.Resigned or EmployeeStatus.Terminated)
            throw new ErrorException(409, "EMPLOYEE_UNAVAILABLE", "A departed employee cannot be promoted.");
        var position = await Repository<Position>().Entities.SingleOrDefaultAsync(p => p.Id == promotion.NewPositionId && p.IsActive && !p.DeletedTime.HasValue)
            ?? throw new ErrorException(409, "POSITION_UNAVAILABLE", "The approved position is no longer active.");
        if (!AssignableRoles.Contains(promotion.TargetRoleName)) throw new ErrorException(409, "INVALID_ROLE", "The approved role is invalid.");
        var prefix = string.IsNullOrWhiteSpace(position.Code) ? "EMP" : position.Code.Trim().ToUpperInvariant();
        var codes = await Repository<Employee>().Entities.Select(e => e.EmployeeCode).ToListAsync();
        var suffix = codes.Where(c => c.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase))
            .Select(c => int.TryParse(c[(c.LastIndexOf('-') + 1)..], out var n) ? n : 0).DefaultIfEmpty().Max() + 1;
        var code = $"{prefix}-{suffix:D3}";
        while (codes.Contains(code, StringComparer.OrdinalIgnoreCase)) code = $"{prefix}-{++suffix:D3}";
        if (employee.UserId is Guid userId)
        {
            var user = await users.FindByIdAsync(userId.ToString());
            if (user == null || user.DeletedTime.HasValue) throw new ErrorException(409, "USER_UNAVAILABLE", "The linked account is unavailable.");
            var current = await users.GetRolesAsync(user);
            foreach (var role in current.Where(r => AssignableRoles.Contains(r) && r != "Employee" && r != promotion.TargetRoleName))
                Ensure(await users.RemoveFromRoleAsync(user, role));
            if (!current.Contains("Employee")) Ensure(await users.AddToRoleAsync(user, "Employee"));
            if (!current.Contains(promotion.TargetRoleName)) Ensure(await users.AddToRoleAsync(user, promotion.TargetRoleName));
            Ensure(await users.UpdateSecurityStampAsync(user));
        }
        else if (promotion.TargetRoleName != "Employee") throw new ErrorException(409, "USER_REQUIRED", "The approved role requires an account.");
        employee.EmployeeCode = code;
        employee.PositionId = position.Id;
        employee.LastUpdatedBy = "System:EffectiveDate";
        employee.LastUpdatedTime = UtcNow;
        promotion.NewEmployeeCode = code;
        promotion.AppliedAt = UtcNow.UtcDateTime;
        promotion.LastUpdatedBy = "System:EffectiveDate";
        promotion.LastUpdatedTime = UtcNow;
    }

    public async Task ApplyHandoverIfDueAsync(EmployeeHandover handover)
    {
        if (handover.Status != EmployeeHandoverStatus.Approved || handover.AppliedAt.HasValue || handover.LastWorkingDate.Date >= Today) return;
        var employee = await Repository<Employee>().Entities.SingleOrDefaultAsync(e => e.Id == handover.EmployeeId && !e.DeletedTime.HasValue)
            ?? throw new ErrorException(409, "EMPLOYEE_UNAVAILABLE", "The approved employee profile is unavailable.");
        if (employee.UserId is Guid userId)
        {
            await adminGuard.EnsureAnotherActiveAdminAsync(userId);
            var user = await users.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                Ensure(await users.UpdateAsync(user));
                Ensure(await users.UpdateSecurityStampAsync(user));
            }
        }
        employee.Status = EmployeeStatus.Resigned;
        employee.LastUpdatedBy = "System:EffectiveDate";
        employee.LastUpdatedTime = UtcNow;
        handover.AppliedAt = UtcNow.UtcDateTime;
        handover.LastUpdatedBy = "System:EffectiveDate";
        handover.LastUpdatedTime = UtcNow;
    }

    public async Task<int> ApplyDueAsync(CancellationToken cancellationToken = default)
    {
        var today = Today;
        var handovers = await Repository<EmployeeHandover>().Entities.AsNoTracking()
            .Where(h => !h.DeletedTime.HasValue && h.Status == EmployeeHandoverStatus.Approved && h.AppliedAt == null && h.LastWorkingDate < today)
            .OrderBy(h => h.LastWorkingDate).ThenBy(h => h.Id).Select(h => h.Id).Take(100).ToListAsync(cancellationToken);
        var promotions = await Repository<EmployeePromotion>().Entities.AsNoTracking()
            .Where(p => !p.DeletedTime.HasValue && p.Status == EmployeePromotionStatus.Approved && p.AppliedAt == null && p.EffectiveDate <= today)
            .OrderBy(p => p.EffectiveDate).ThenBy(p => p.Id).Select(p => p.Id).Take(100).ToListAsync(cancellationToken);
        var applied = 0;
        // Departure takes priority over a promotion due on the same day.
        foreach (var (id, handover) in handovers.Select(id => (id, true)).Concat(promotions.Select(id => (id, false))))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                applied += await InTransactionAsync(async () =>
                {
                    if (handover)
                    {
                        var row = await Repository<EmployeeHandover>().Entities.SingleAsync(h => h.Id == id, cancellationToken);
                        if (row.AppliedAt.HasValue) return 0;
                        await ApplyHandoverIfDueAsync(row);
                        await UnitOfWork.SaveAsync(cancellationToken);
                        return row.AppliedAt.HasValue ? 1 : 0;
                    }
                    var promotion = await Repository<EmployeePromotion>().Entities.SingleAsync(p => p.Id == id, cancellationToken);
                    if (promotion.AppliedAt.HasValue) return 0;
                    await ApplyPromotionIfDueAsync(promotion);
                    await UnitOfWork.SaveAsync(cancellationToken);
                    return promotion.AppliedAt.HasValue ? 1 : 0;
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception error) { logger.LogError(error, "Effective date change {ChangeId} ({Kind}) failed; left unapplied for retry", id, handover ? "handover" : "promotion"); }
        }
        return applied;
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new ErrorException(409, "ACCOUNT_UPDATE_FAILED", "Could not apply the approved account change.");
    }
}
