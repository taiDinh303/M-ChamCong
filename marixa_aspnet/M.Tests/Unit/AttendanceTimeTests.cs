using M.Contract.Repositories.Entities;
using M.Services.Services.Timekeeping;

namespace M.Tests.Unit;

public sealed class AttendanceTimeTests
{
    [Fact]
    public void Overtime_is_intersection_of_actual_work_and_merged_approved_windows()
    {
        var start = DateTimeOffset.Parse("2026-10-17T08:00:00+07:00");
        var end = start.AddHours(10);
        var windows = new[]
        {
            Window("17:00", "19:00"), Window("17:30", "18:30"),
            Window("18:00", "20:00", OvertimeStatus.Pending),
            Window("15:00", "16:00", OvertimeStatus.Rejected)
        };
        var result = AttendanceTimeCalculator.Compute(start, end, new AttendanceRule(), windows);
        Assert.Equal(8m, result.ActualHours);
        Assert.Equal(1m, result.OvertimeHours);
        windows[0].OvertimeHours = 999;
        Assert.Equal(result, AttendanceTimeCalculator.Compute(start, end, new AttendanceRule(), windows));
    }

    [Fact]
    public void Configured_break_is_subtracted_only_where_work_overlaps_and_minutes_are_retained()
    {
        var rule = new AttendanceRule { CheckInTime = new(9, 0), CheckOutTime = new(18, 0), BreakStartTime = new(12, 30), BreakMinutes = 60 };
        var result = AttendanceTimeCalculator.Compute(DateTimeOffset.Parse("2026-10-17T09:20:00+07:00"),
            DateTimeOffset.Parse("2026-10-17T18:45:00+07:00"), rule, [Window("18:00", "19:00")]);
        Assert.Equal(7.67m, result.ActualHours);
        Assert.Equal(0.75m, result.OvertimeHours);
        var partial = AttendanceTimeCalculator.Compute(DateTimeOffset.Parse("2026-10-17T12:45:00+07:00"),
            DateTimeOffset.Parse("2026-10-17T14:00:00+07:00"), rule);
        Assert.Equal(0.5m, partial.ActualHours);
    }

    [Fact]
    public void Approved_time_inside_the_shift_is_not_counted_twice()
    {
        var result = AttendanceTimeCalculator.Compute(DateTimeOffset.Parse("2026-10-17T08:00:00+07:00"),
            DateTimeOffset.Parse("2026-10-17T18:00:00+07:00"), new AttendanceRule(), [Window("16:30", "18:00")]);
        Assert.Equal(7.5m, result.ActualHours);
        Assert.Equal(1.5m, result.OvertimeHours);
    }

    [Fact]
    public void Missing_or_reversed_actual_times_do_not_invent_hours()
    {
        Assert.Equal((null, null), AttendanceTimeCalculator.Compute(null, DateTimeOffset.UtcNow, new AttendanceRule()));
        var time = DateTimeOffset.Parse("2026-10-17T01:00:00Z");
        Assert.Equal((null, null), AttendanceTimeCalculator.Compute(time, time.AddMinutes(-1), new AttendanceRule()));
    }

    private static OvertimeRequest Window(string start, string end, OvertimeStatus status = OvertimeStatus.Approved) =>
        new() { StartAt = DateTimeOffset.Parse($"2026-10-17T{start}:00+07:00"), EndAt = DateTimeOffset.Parse($"2026-10-17T{end}:00+07:00"), Status = status };
}
