using System.ComponentModel.DataAnnotations;
using M.Contract.Repositories.Entities;

namespace M.ModelViews.AttendancePeriodModelView;

public sealed class PeriodActionRequest
{
    [Required, MinLength(5), MaxLength(1000)] public string Reason { get; set; } = "";
}

public sealed record PeriodView(int Year, int Month, AttendancePeriodState State, int CurrentVersion,
    Guid? ReviewedBy, DateTimeOffset? ReviewedAt, string? ReviewNote, DateTimeOffset? LockedAt);

public sealed record SnapshotRow(Guid AttendanceId, Guid EmployeeId, string EmployeeCode, string EmployeeName,
    DateTime Date, AttendanceStatus? Status, AttendanceApprovalStatus ApprovalStatus,
    DateTimeOffset? CheckIn, DateTimeOffset? CheckOut, decimal? ActualHours, decimal? OvertimeHours);

public sealed record SnapshotRule(string Code, TimeOnly CheckIn, TimeOnly CheckOut, int BreakMinutes, int StandardHours)
{
    public TimeOnly BreakStartTime { get; init; } = new(12, 0);
}
public sealed record PeriodSnapshot(int Year, int Month, int Version, DateTimeOffset CapturedAt,
    SnapshotRule Rule, IReadOnlyList<SnapshotRow> Rows);
public sealed record SnapshotView(PeriodSnapshot Data, string Sha256);
public sealed record PeriodEventView(string Action, Guid? ActorUserId, string ActorName, string Reason, DateTimeOffset At);

public sealed record LeavePdfData(Guid Id, string EmployeeCode, string EmployeeName, string LeaveType,
    DateTime FromDate, DateTime ToDate, decimal? TotalDays, string? Reason, LeaveRequestStatus Status,
    string? Approver, DateTime? ApprovedAt, DateTimeOffset DataVersion, DateTimeOffset ExportedAt)
{
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
}
