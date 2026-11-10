using System.Text.Json;
using M.Contract.Repositories.Entities;
using M.Core.Base;
using Microsoft.EntityFrameworkCore;

namespace M.Repositories.Context;

public partial class DatabaseContext
{
    public string? WorkforceChangeReason { get; set; }
    private static bool IsWorkforceRecord(object value) => value is Department or Position or Bank
        or LeaveType or HolidayCalendar or EmployeeContract;

    private void CaptureWorkforceHistory()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<AuditLog>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new BaseException.ErrorException(409, "IMMUTABLE_AUDIT", "Audit history is append-only.");
        foreach (var entry in ChangeTracker.Entries().Where(e => IsWorkforceRecord(e.Entity)
                     && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            if (entry.State == EntityState.Deleted)
                throw new BaseException.ErrorException(409, "ARCHIVE_REQUIRED", "Archive workforce records instead of deleting them.");
            var entity = (BaseEntity)entry.Entity;
            var added = entry.State == EntityState.Added;
            var before = added ? null : entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
            var now = _clock.GetUtcNow();
            if (!added && now <= (DateTimeOffset)entry.OriginalValues[nameof(BaseEntity.LastUpdatedTime)]!)
                now = ((DateTimeOffset)entry.OriginalValues[nameof(BaseEntity.LastUpdatedTime)]!).AddMilliseconds(1);
            entity.LastUpdatedTime = now;
            var after = entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
            AuditLogs.Add(new AuditLog
            {
                EntityType = entry.Metadata.ClrType.Name, EntityId = entity.Id,
                ActorUserName = _actor?.Name ?? "System", ActorEmployeeId = _actor?.EmployeeId,
                Action = added ? AuditAction.Create : entity.DeletedTime.HasValue ? AuditAction.Delete : AuditAction.Update,
                BeforeJson = before == null ? null : JsonSerializer.Serialize(before),
                AfterJson = JsonSerializer.Serialize(after), Reason = WorkforceChangeReason,
                CreatedTime = now, LastUpdatedTime = now, CreatedBy = _actor?.Name ?? "System"
            });
        }
    }
}
