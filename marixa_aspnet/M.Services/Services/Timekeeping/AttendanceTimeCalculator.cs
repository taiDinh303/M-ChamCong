using M.Contract.Repositories.Entities;

namespace M.Services.Services.Timekeeping;

/// <summary>Owner decision 2026-10-17: normal shift time and approved overtime are separate, minute-based quantities.</summary>
public static class AttendanceTimeCalculator
{
    public static (decimal? ActualHours, decimal? OvertimeHours) Compute(DateTimeOffset? checkIn,
        DateTimeOffset? checkOut, AttendanceRule? rule, IEnumerable<OvertimeRequest>? overtime = null)
    {
        if (!checkIn.HasValue || !checkOut.HasValue || checkOut < checkIn) return (null, null);
        var start = checkIn.Value;
        var end = checkOut.Value;
        if (start == end) return (0m, 0m);
        var day = start.ToOffset(TimeSpan.FromHours(7)).Date;
        DateTimeOffset At(TimeOnly time) => new(day.Add(time.ToTimeSpan()), TimeSpan.FromHours(7));
        var shiftStart = At(rule?.CheckInTime ?? new TimeOnly(8, 0));
        var shiftEnd = At(rule?.CheckOutTime ?? new TimeOnly(17, 0));
        if (shiftEnd <= shiftStart) shiftEnd = shiftEnd.AddDays(1);
        var breakStart = At(rule?.BreakStartTime ?? new TimeOnly(12, 0));
        if (breakStart < shiftStart) breakStart = breakStart.AddDays(1);
        var breakEnd = breakStart.AddMinutes(Math.Max(0, rule?.BreakMinutes ?? 60));
        var windows = Merge((overtime ?? []).Where(r => r.Status == OvertimeStatus.Approved && !r.DeletedTime.HasValue
                && r.StartAt.HasValue && r.EndAt > r.StartAt)
            .Select(r => (Start: Max(start, r.StartAt!.Value), End: Min(end, r.EndAt!.Value)))
            .Where(w => w.End > w.Start));
        var normalStart = Max(start, shiftStart);
        var normalEnd = Min(end, shiftEnd);
        var normalMinutes = NetMinutes(normalStart, normalEnd, breakStart, breakEnd);
        // An approved OT interval inside the shift is excluded from normal hours, so it is never counted twice.
        normalMinutes -= windows.Sum(w => NetMinutes(Max(normalStart, w.Start), Min(normalEnd, w.End), breakStart, breakEnd));
        normalMinutes = Math.Clamp(normalMinutes, 0, Math.Max(0, rule?.StandardHours ?? 8) * 60m);
        var overtimeMinutes = windows.Sum(w => NetMinutes(w.Start, w.End, breakStart, breakEnd));
        return (Hours(normalMinutes), Hours(overtimeMinutes));
    }

    public static void Enrich(Attendance attendance, AttendanceRule? rule, IEnumerable<OvertimeRequest>? overtime = null)
    {
        var logs = attendance.AttendanceLogs.Where(l => !l.DeletedTime.HasValue);
        var checkIn = attendance.CheckInTime ?? logs.Where(l => l.Type == AttendanceLogType.CheckIn).Select(l => (DateTimeOffset?)l.LogTime).Min();
        var checkOut = attendance.CheckOutTime ?? logs.Where(l => l.Type == AttendanceLogType.CheckOut).Select(l => (DateTimeOffset?)l.LogTime).Max();
        var hours = Compute(checkIn, checkOut, rule, overtime);
        attendance.ActualHours = hours.ActualHours;
        attendance.OvertimeHours = hours.OvertimeHours;
    }

    private static decimal Hours(decimal minutes) => Math.Round(decimal.Floor(Math.Max(0, minutes)) / 60m, 2, MidpointRounding.AwayFromZero);
    private static decimal NetMinutes(DateTimeOffset start, DateTimeOffset end, DateTimeOffset breakStart, DateTimeOffset breakEnd) =>
        Minutes(start, end) - Minutes(Max(start, breakStart), Min(end, breakEnd));
    private static decimal Minutes(DateTimeOffset start, DateTimeOffset end) => Math.Max(0, (decimal)(end - start).TotalMinutes);
    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    private static IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> Merge(IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> input)
    {
        var output = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        foreach (var item in input.OrderBy(w => w.Start))
        {
            if (output.Count == 0 || item.Start > output[^1].End) output.Add(item);
            else output[^1] = (output[^1].Start, Max(output[^1].End, item.End));
        }
        return output;
    }
}
