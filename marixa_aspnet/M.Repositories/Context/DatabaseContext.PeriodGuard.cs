using System.Data;
using M.Contract.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using static M.Core.Base.BaseException;

namespace M.Repositories.Context;

public partial class DatabaseContext
{
    private int Persist(bool accept)
    {
        if (Database.IsNpgsql()) NormalizeDateTimePropertiesToUtc();
        EnsureHistoryIsAppendOnly();
        var keys = AffectedMonths();
        if (keys.Count == 0) return base.SaveChanges(accept);
        using var transaction = Database.CurrentTransaction == null
            ? Database.BeginTransaction(IsolationLevel.Serializable) : null;
        try
        {
            GuardPeriods(AttendancePeriods.Where(p => keys.Contains(p.YearMonth)).ToList());
            var count = base.SaveChanges(accept);
            transaction?.Commit();
            return count;
        }
        catch
        {
            if (transaction != null) { transaction.Rollback(); ChangeTracker.Clear(); }
            throw;
        }
    }

    private async Task<int> PersistAsync(bool accept, CancellationToken cancellationToken)
    {
        if (Database.IsNpgsql()) NormalizeDateTimePropertiesToUtc();
        EnsureHistoryIsAppendOnly();
        var keys = AffectedMonths();
        if (keys.Count == 0) return await base.SaveChangesAsync(accept, cancellationToken);
        await using var transaction = Database.CurrentTransaction == null
            ? await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        try
        {
            GuardPeriods(await AttendancePeriods.Where(p => keys.Contains(p.YearMonth)).ToListAsync(cancellationToken));
            var count = await base.SaveChangesAsync(accept, cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            return count;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                ChangeTracker.Clear();
            }
            throw;
        }
    }

    private void EnsureHistoryIsAppendOnly()
    {
        if (ChangeTracker.Entries<LeaveLedger>().Any(e => e.State is EntityState.Modified or EntityState.Deleted
                && ((LeaveLedgerEntryType)e.OriginalValues[nameof(M.Contract.Repositories.Entities.LeaveLedger.EntryType)]! is LeaveLedgerEntryType.Deduct or LeaveLedgerEntryType.Refund)))
            throw new ErrorException(409, "IMMUTABLE_LEAVE_LEDGER", "Deductions and refunds are append-only audit history.");
        if (ChangeTracker.Entries<AttendanceLog>().Any(e => e.State == EntityState.Deleted
                || (e.State == EntityState.Modified && e.Properties.Any(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)))))
            throw new ErrorException(409, "IMMUTABLE_ATTENDANCE_LOG", "Raw attendance events cannot be changed or deleted. Submit an attendance correction.");
        if (ChangeTracker.Entries().Any(e => e.Entity is AttendancePeriodSnapshot or AttendancePeriodEvent
                && e.State is EntityState.Modified or EntityState.Deleted))
            throw new ErrorException(409, "IMMUTABLE_SNAPSHOT", "Period snapshots and events are append-only.");
        if (ChangeTracker.Entries<AttendancePeriod>().Any(e => e.State == EntityState.Deleted || e.Entity.DeletedTime.HasValue))
            throw new ErrorException(409, "IMMUTABLE_PERIOD", "Attendance periods cannot be deleted.");
    }

    private void GuardPeriods(IEnumerable<AttendancePeriod> periods)
    {
        foreach (var period in periods)
        {
            if (period.State == AttendancePeriodState.Locked) throw AttendancePeriod.Locked();
            if (period.State != AttendancePeriodState.Reviewed) continue;
            period.InvalidateReview();
            AttendancePeriodEvents.Add(AttendancePeriodEvent.Record(period.Id, "ReviewInvalidated", _actor?.UserId,
                _actor?.Name ?? "System", "Source data changed; HR review is required again.", _clock.GetUtcNow()));
        }
    }

    private HashSet<int> AffectedMonths()
    {
        var keys = new HashSet<int>();
        var logParents = new HashSet<Guid>();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray())
        {
            switch (entry.Entity)
            {
                case Attendance attendance:
                    Add(attendance.AttendanceDate);
                    if (entry.State != EntityState.Added) Add((DateTime)entry.OriginalValues[nameof(Attendance.AttendanceDate)]!);
                    break;
                case AttendanceLog log:
                    logParents.Add(log.AttendanceId);
                    if (entry.State != EntityState.Added) logParents.Add((Guid)entry.OriginalValues[nameof(AttendanceLog.AttendanceId)]!);
                    break;
                case LeaveRequest leave:
                    Range(leave.FromDate, leave.ToDate);
                    if (entry.State != EntityState.Added)
                        Range((DateTime)entry.OriginalValues[nameof(LeaveRequest.FromDate)]!, (DateTime)entry.OriginalValues[nameof(LeaveRequest.ToDate)]!);
                    break;
                case OvertimeRequest overtime:
                    Add(overtime.WorkDate);
                    if (entry.State != EntityState.Added) Add((DateTime)entry.OriginalValues[nameof(OvertimeRequest.WorkDate)]!);
                    break;
                case AttendanceCorrection correction:
                    Add(correction.WorkDate);
                    if (entry.State != EntityState.Added) Add((DateTime)entry.OriginalValues[nameof(AttendanceCorrection.WorkDate)]!);
                    break;
            }
        }
        if (logParents.Count > 0)
        {
            foreach (var parent in ChangeTracker.Entries<Attendance>().Where(e => logParents.Contains(e.Entity.Id)))
                Add(parent.Entity.AttendanceDate);
            foreach (var date in Attendances.Where(a => logParents.Contains(a.Id)).Select(a => a.AttendanceDate)) Add(date);
        }
        if (ChangeTracker.Entries<AttendanceRule>().Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            foreach (var key in AttendancePeriods.Where(p => p.State == AttendancePeriodState.Reviewed).Select(p => p.YearMonth))
                keys.Add(key);
        return keys;

        void Add(DateTime date) => keys.Add(date.Year * 100 + date.Month);
        void Range(DateTime from, DateTime to)
        {
            if (to < from) return;
            if (to.Year - from.Year > 100) throw new ErrorException(400, "INVALID_DATE_RANGE", "The date range is too large.");
            for (var day = new DateTime(from.Year, from.Month, 1); day <= to;)
            {
                Add(day);
                if (day.Year == to.Year && day.Month == to.Month) break;
                day = day.AddMonths(1);
            }
        }
    }
}
