using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using static M.Core.Base.BaseException;

namespace M.Contract.Repositories.Entities;

public enum AttendancePeriodState { Open, Reviewed, Locked }

public sealed class AttendancePeriod : BaseEntity
{
    private AttendancePeriod() { }
    public int YearMonth { get; private set; }
    public AttendancePeriodState State { get; private set; }
    public int CurrentVersion { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    [MaxLength(1000)] public string? ReviewNote { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }

    public static AttendancePeriod Open(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            throw new ErrorException(400, "INVALID_PERIOD", "Year must be 2000–2100 and month 1–12.");
        return new AttendancePeriod { YearMonth = year * 100 + month };
    }

    public void Review(Guid actorId, DateTimeOffset now, string note)
    {
        if (State == AttendancePeriodState.Locked) throw Locked();
        State = AttendancePeriodState.Reviewed;
        ReviewedBy = actorId;
        ReviewedAt = now;
        ReviewNote = note;
    }

    public int Lock(DateTimeOffset now)
    {
        if (State != AttendancePeriodState.Reviewed)
            throw new ErrorException(409, "PERIOD_NOT_REVIEWED", "HR review is required before locking.");
        State = AttendancePeriodState.Locked;
        LockedAt = now;
        return ++CurrentVersion;
    }

    public void Reopen()
    {
        if (State != AttendancePeriodState.Locked)
            throw new ErrorException(409, "PERIOD_NOT_LOCKED", "Only a locked period may be reopened.");
        State = AttendancePeriodState.Open;
        ReviewedBy = null;
        ReviewedAt = null;
        ReviewNote = null;
        LockedAt = null;
    }

    public void InvalidateReview()
    {
        if (State != AttendancePeriodState.Reviewed) return;
        State = AttendancePeriodState.Open;
        ReviewedBy = null;
        ReviewedAt = null;
        ReviewNote = null;
    }

    public static ErrorException Locked() => new(409, "PERIOD_LOCKED", "The attendance period is locked. An administrator must reopen it with a reason.");
}

public sealed class AttendancePeriodSnapshot : BaseEntity
{
    private AttendancePeriodSnapshot() { }
    public Guid PeriodId { get; private set; }
    public int Version { get; private set; }
    public string PayloadJson { get; private set; } = "";
    [MaxLength(64)] public string Sha256 { get; private set; } = "";

    public static AttendancePeriodSnapshot Capture(Guid periodId, int version, string json, string hash,
        string actor, DateTimeOffset now) => new()
    {
        PeriodId = periodId, Version = version, PayloadJson = json, Sha256 = hash,
        CreatedBy = actor, CreatedTime = now
    };
}

public sealed class AttendancePeriodEvent : BaseEntity
{
    public Guid PeriodId { get; private set; }
    [MaxLength(40)] public string Action { get; private set; } = "";
    public Guid? ActorUserId { get; private set; }
    [MaxLength(200)] public string ActorName { get; private set; } = "";
    [MaxLength(1000)] public string Reason { get; private set; } = "";

    public static AttendancePeriodEvent Record(Guid periodId, string action, Guid? actorId,
        string name, string reason, DateTimeOffset now) => new()
    {
        PeriodId = periodId, Action = action, ActorUserId = actorId, ActorName = name,
        Reason = reason, CreatedBy = name, CreatedTime = now
    };
}
