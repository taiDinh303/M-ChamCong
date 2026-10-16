namespace M.Core.Time;

/// <summary>Vietnam business dates, never dependent on the host's local timezone.</summary>
public static class BusinessTime
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(7);

    public static DateOnly DateAt(DateTimeOffset instant) =>
        DateOnly.FromDateTime(instant.ToOffset(UtcOffset).DateTime);

    // The existing schema stores a business-date label in a DateTime column.
    // UTC Kind satisfies Npgsql; this label is not the instant at VN midnight.
    public static DateTime DateLabelAt(DateTimeOffset instant) =>
        DateTime.SpecifyKind(DateAt(instant).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
}
