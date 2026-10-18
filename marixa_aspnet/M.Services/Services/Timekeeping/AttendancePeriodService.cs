using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Export;
using M.Contract.Services.Interface;
using M.Contract.Services.Storage;
using M.Core.Security;
using M.ModelViews.AttendancePeriodModelView;
using Microsoft.EntityFrameworkCore;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Timekeeping;

public sealed class AttendancePeriodService(IUnitOfWork unitOfWork, ICurrentUser actor, TimeProvider clock,
    IWorkforceExportWriter exports) : BaseService(unitOfWork, actor, clock), IAttendancePeriodService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PeriodView> GetAsync(int year, int month)
    {
        RequireStaff();
        return View(await FindAsync(year, month) ?? AttendancePeriod.Open(year, month));
    }

    public async Task<IReadOnlyList<PeriodEventView>> HistoryAsync(int year, int month)
    {
        RequireStaff();
        var period = await FindAsync(year, month);
        if (period == null) return [];
        return await Repository<AttendancePeriodEvent>().Entities.Where(e => e.PeriodId == period.Id)
            .OrderBy(e => e.CreatedTime).Select(e => new PeriodEventView(e.Action, e.ActorUserId, e.ActorName, e.Reason, e.CreatedTime))
            .ToListAsync();
    }

    public Task<PeriodView> ReviewAsync(int year, int month, string reason)
    {
        RequireReviewer();
        ValidateReason(reason);
        return InTransactionAsync(async () =>
        {
            var period = await GetOrOpenAsync(year, month);
            if (period.State == AttendancePeriodState.Locked) throw AttendancePeriod.Locked();
            var rows = await ReadRowsAsync(year, month);
            ValidateRows(rows);
            await ValidateRequestsAsync(year, month);
            await RuleAsync();
            period.Review(ActorId(), UtcNow, reason.Trim());
            Record(period, "Reviewed", reason);
            await UnitOfWork.SaveAsync();
            return View(period);
        });
    }

    public Task<PeriodView> LockAsync(int year, int month, string reason)
    {
        RequireAdmin();
        ValidateReason(reason);
        return InTransactionAsync(async () =>
        {
            var period = await GetOrOpenAsync(year, month);
            if (period.State != AttendancePeriodState.Reviewed)
                throw new ErrorException(409, "PERIOD_NOT_REVIEWED", "HR review is required before locking.");
            var rows = await ReadRowsAsync(year, month);
            ValidateRows(rows);
            await ValidateRequestsAsync(year, month);
            var rule = await RuleAsync();
            var overtime = await ApprovedOvertimeAsync(year, month);
            var version = period.Lock(UtcNow);
            var data = new PeriodSnapshot(year, month, version, UtcNow,
                new SnapshotRule(rule.Code, rule.CheckInTime, rule.CheckOutTime, rule.BreakMinutes, rule.StandardHours) { BreakStartTime = rule.BreakStartTime },
                rows.Select(a => Row(a, rule, overtime.Where(r => r.EmployeeId == a.EmployeeId && r.WorkDate.Date == a.AttendanceDate.Date))).ToArray());
            var json = JsonSerializer.Serialize(data, Json);
            Repository<AttendancePeriodSnapshot>().Insert(AttendancePeriodSnapshot.Capture(period.Id,
                version, json, Hash(json), CurrentUserName, UtcNow));
            Record(period, "Locked", reason);
            await UnitOfWork.SaveAsync();
            return View(period);
        });
    }

    public Task<PeriodView> ReopenAsync(int year, int month, string reason)
    {
        RequireAdmin();
        ValidateReason(reason);
        return InTransactionAsync(async () =>
        {
            var period = await FindAsync(year, month)
                ?? throw new ErrorException(404, "NOT_FOUND", "Attendance period not found.");
            period.Reopen();
            Record(period, "Reopened", reason);
            await UnitOfWork.SaveAsync();
            return View(period);
        });
    }

    public async Task<SnapshotView> SnapshotAsync(int year, int month, int? version = null)
    {
        if (!CurrentUser.IsAuthenticated) throw new ErrorException(401, "UNAUTHORIZED", "Authentication required.");
        var period = await FindAsync(year, month)
            ?? throw new ErrorException(404, "NOT_FOUND", "Attendance period not found.");
        var selected = version ?? period.CurrentVersion;
        if (selected <= 0) throw new ErrorException(404, "SNAPSHOT_NOT_FOUND", "No locked snapshot is available.");
        var snapshot = await Repository<AttendancePeriodSnapshot>().Entities.AsNoTracking()
            .SingleOrDefaultAsync(s => s.PeriodId == period.Id && s.Version == selected)
            ?? throw new ErrorException(404, "SNAPSHOT_NOT_FOUND", "Snapshot version not found.");
        // Hex casing does not change the digest. Keep immutable historical snapshots readable.
        if (!string.Equals(Hash(snapshot.PayloadJson), snapshot.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new ErrorException(409, "SNAPSHOT_INTEGRITY", "Snapshot integrity check failed.");
        var data = JsonSerializer.Deserialize<PeriodSnapshot>(snapshot.PayloadJson, Json)
            ?? throw new ErrorException(409, "SNAPSHOT_INTEGRITY", "Snapshot content is invalid.");
        if (!IsStaff)
        {
            var employeeId = RequireCurrentEmployeeId();
            data = data with { Rows = data.Rows.Where(row => row.EmployeeId == employeeId).ToArray() };
        }
        return new SnapshotView(data, Hash(JsonSerializer.Serialize(data, Json)));
    }

    public async Task<DocumentDownload> ExportAsync(int year, int month, int? version = null)
    {
        var snapshot = await SnapshotAsync(year, month, version);
        return new DocumentDownload(new MemoryStream(exports.AttendanceXlsx(snapshot.Data, snapshot.Sha256)),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Marixa-{year:D4}{month:D2}-v{snapshot.Data.Version}.xlsx");
    }

    private Task<AttendancePeriod?> FindAsync(int year, int month)
    {
        var key = AttendancePeriod.Open(year, month).YearMonth;
        return Repository<AttendancePeriod>().Entities.SingleOrDefaultAsync(p => p.YearMonth == key);
    }

    private async Task<AttendancePeriod> GetOrOpenAsync(int year, int month)
    {
        var period = await FindAsync(year, month);
        if (period != null) return period;
        period = AttendancePeriod.Open(year, month);
        period.CreatedBy = CurrentUserName;
        period.CreatedTime = UtcNow;
        Repository<AttendancePeriod>().Insert(period);
        return period;
    }

    private Task<List<Attendance>> ReadRowsAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return Repository<Attendance>().Entities.AsNoTracking().Include(a => a.Employee).Include(a => a.AttendanceLogs)
            .Where(a => !a.DeletedTime.HasValue && a.AttendanceDate >= start && a.AttendanceDate < end)
            .OrderBy(a => a.EmployeeId).ThenBy(a => a.AttendanceDate).ToListAsync();
    }

    private async Task<AttendanceRule> RuleAsync()
    {
        var rules = await Repository<AttendanceRule>().Entities.AsNoTracking().Where(r => r.IsActive && !r.DeletedTime.HasValue)
            .Take(2).ToListAsync();
        if (rules.Count != 1) throw new ErrorException(409, "RULE_REQUIRED", "Exactly one active attendance rule is required.");
        return rules[0];
    }

    private static void ValidateRows(List<Attendance> rows)
    {
        if (rows.Count == 0) throw new ErrorException(409, "EMPTY_PERIOD", "There are no attendance records to review.");
        if (rows.Any(a => a.ApprovalStatus != AttendanceApprovalStatus.Approved || !a.Status.HasValue || !Enum.IsDefined(a.Status.Value)))
            throw new ErrorException(409, "UNREVIEWED_ATTENDANCE", "Every attendance record must have an approved status before closing.");
    }

    private async Task ValidateRequestsAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        if (await Repository<LeaveRequest>().Entities.AnyAsync(r => !r.DeletedTime.HasValue
                && r.Status == LeaveRequestStatus.Pending && r.FromDate < end && r.ToDate >= start)
            || await Repository<OvertimeRequest>().Entities.AnyAsync(r => !r.DeletedTime.HasValue
                && r.Status == OvertimeStatus.Pending && r.WorkDate >= start && r.WorkDate < end)
            || await Repository<AttendanceCorrection>().Entities.AnyAsync(r => !r.DeletedTime.HasValue
                && r.Status == AttendanceCorrectionStatus.Pending && r.WorkDate >= start && r.WorkDate < end))
            throw new ErrorException(409, "PENDING_REQUESTS", "Resolve pending leave, overtime and correction requests before closing the period.");
        if (await Repository<OvertimeRequest>().Entities.AnyAsync(r => !r.DeletedTime.HasValue
                && r.Status == OvertimeStatus.Approved && r.WorkDate >= start && r.WorkDate < end
                && (r.StartAt == null || r.EndAt == null || r.EndAt <= r.StartAt)))
            throw new ErrorException(409, "INVALID_APPROVED_OVERTIME", "An approved overtime request has no valid time window. Repair it with an audited data correction before closing.");
    }

    private Task<List<OvertimeRequest>> ApprovedOvertimeAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return Repository<OvertimeRequest>().Entities.AsNoTracking().Where(r => r.WorkDate >= start && r.WorkDate < end
            && !r.DeletedTime.HasValue && r.Status == OvertimeStatus.Approved).ToListAsync();
    }

    private static SnapshotRow Row(Attendance a, AttendanceRule rule, IEnumerable<OvertimeRequest> overtime)
    {
        var logs = a.AttendanceLogs.Where(l => !l.DeletedTime.HasValue);
        var checkIn = a.CheckInTime ?? logs.Where(l => l.Type == AttendanceLogType.CheckIn).Select(l => (DateTimeOffset?)l.LogTime).Min();
        var checkOut = a.CheckOutTime ?? logs.Where(l => l.Type == AttendanceLogType.CheckOut).Select(l => (DateTimeOffset?)l.LogTime).Max();
        var hours = AttendanceTimeCalculator.Compute(checkIn, checkOut, rule, overtime);
        return new SnapshotRow(a.Id, a.EmployeeId, a.Employee?.EmployeeCode ?? "", a.Employee?.FullName ?? "",
            DateTime.SpecifyKind(a.AttendanceDate.Date, DateTimeKind.Utc), a.Status, a.ApprovalStatus,
            checkIn, checkOut, hours.ActualHours, hours.OvertimeHours);
    }

    private void Record(AttendancePeriod period, string action, string reason) =>
        Repository<AttendancePeriodEvent>().Insert(AttendancePeriodEvent.Record(period.Id, action,
            ActorId(), CurrentUserName, reason.Trim(), UtcNow));
    private Guid ActorId() => CurrentUser.UserId ?? throw new ErrorException(403, "FORBIDDEN", "An authenticated user account is required.");
    private void RequireReviewer()
    {
        RequireStaff();
        if (!CurrentUser.IsInRole(RoleNames.Admin) && !CurrentUser.IsInRole(RoleNames.HR))
            throw new ErrorException(403, "FORBIDDEN", "HR or administrator access is required.");
    }
    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 1000)
            throw new ErrorException(400, "VALIDATION", "A reason of 5–1000 characters is required.");
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static PeriodView View(AttendancePeriod period) => new(period.YearMonth / 100, period.YearMonth % 100,
        period.State, period.CurrentVersion, period.ReviewedBy, period.ReviewedAt, period.ReviewNote, period.LockedAt);
}
