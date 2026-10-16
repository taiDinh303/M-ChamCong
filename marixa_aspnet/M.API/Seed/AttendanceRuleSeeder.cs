using M.Contract.Repositories.Entities;
using M.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace M.API.Seed
{
    /// <summary>
    /// Đảm bảo luôn tồn tại 1 quy định chấm công mặc định "Ca hành chính"
    /// (làm việc 8:00 -> 17:00) để trang "Quy định" có nội dung hiển thị.
    /// Idempotent: chỉ tạo khi chưa có quy tắc nào (hoặc chưa có mã DEFAULT-CADAY).
    /// </summary>
    public static class AttendanceRuleSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("AttendanceRuleSeeder");

            // Seed only when no active rule exists; never overwrite custom or disabled rules.
            var defaultRule = await db.AttendanceRules
                .FirstOrDefaultAsync(r => r.Code == "DEFAULT-CA");

            if (defaultRule is null && !await db.AttendanceRules.AnyAsync(r => r.IsActive && !r.DeletedTime.HasValue))
            {
                db.AttendanceRules.Add(DefaultRule());
                await db.SaveChangesAsync();
                logger.LogInformation("AttendanceRuleSeeder: tạo ca hành chính 8h-17h (nghỉ trưa 1h).");
            }
        }

        private static AttendanceRule DefaultRule() => new()
        {
            Code = "DEFAULT-CA",
            Name = "Ca hành chính",
            Description = "Làm việc từ 08:00 đến 17:00. Nghỉ trưa 12:00 - 13:00 (1 giờ), 13:00 vào làm lại. Vượt quá 17:00 được tính là tăng ca (OT).",
            StandardHours = 8,
            CheckInTime = new TimeOnly(8, 0),
            CheckOutTime = new TimeOnly(17, 0),
            LateGraceMinutes = 0,
            EarlyLeaveThresholdMinutes = 0,
            BreakMinutes = 60,
            PhotoRequired = false,
            GpsRequired = false,
            IsActive = true,
        };
    }
}
