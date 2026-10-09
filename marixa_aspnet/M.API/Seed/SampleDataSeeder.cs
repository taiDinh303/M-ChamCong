using M.Contract.Repositories.Entity;
using M.Contract.Repositories.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using M.Repositories.Context;

namespace M.API.Seed
{
    /// <summary>
    /// Seed 8 tài khoản mẫu (idempotent: chỉ tạo nếu chưa tồn tại).
    /// Chạy khi API khởi động (cả Dev + Production).
    ///
    /// Khi cần THÊM / SỬA dữ liệu mẫu:
    ///   1. Mở mảng Users bên dưới
    ///   2. Thêm dòng mới: ("MÃ_NV", "TÊN_RIÊNG", "HỌ_TÊN_ĐỆM", "Role")
    ///   3. dotnet build → push Render → auto seed (không cần restart DB)
    ///
    /// Role hợp lệ: "Employee", "Manager", "HR", "Admin", "Accountant"
    /// </summary>
    public static class SampleDataSeeder
    {
        // Mật khẩu thống nhất cho tất cả tài khoản mẫu (đổi nếu cần)
        private const string Password = "Hovaten123@";

        /// <summary>
        /// 8 tài khoản mẫu:
        ///   (Mã NV, Tên riêng, Họ + tên đệm, Role Identity)
        /// </summary>
        private static readonly (string Code, string GivenName, string FamilyName, string Role)[] Users =
        {
            ("0900000001", "Tài",   "Đinh Văn",    "Employee"),
            ("0900000002", "Luân",  "Phùng Vĩnh",  "Employee"),
            ("0900000003", "Khoa",  "Lê Anh",      "HR"),
            ("0900000004", "Tuyền", "Trần Phụng",  "HR"),
            ("0900000005", "Đăng",  "Huỳnh Hoàng",  "Manager"),
            ("0900000006", "Tuấn",  "Josept Đức",  "Admin"),
            ("0900000007", "Tuấn",  "Nguyễn Minh", "Accountant"),
            ("0900000008", "Hà",    "Lê Thu",      "Accountant"),
        };

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
            var db = sp.GetRequiredService<DatabaseContext>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");

            // Đảm bảo role "Accountant" tồn tại (RoleSeeder chỉ tạo 4 role cũ)
            if (!await roleManager.RoleExistsAsync("Accountant"))
            {
                var result = await roleManager.CreateAsync(
                    new ApplicationRole { Name = "Accountant", Description = "Kế toán" });
                if (result.Succeeded)
                    logger.LogInformation("Created role: Accountant");
            }

            foreach (var (code, givenName, familyName, role) in Users)
            {
                // Idempotent: user đã tồn tại → skip
                var existing = await userManager.FindByNameAsync(code);
                if (existing != null)
                {
                    logger.LogDebug("SampleDataSeeder: {Code} already exists, skip", code);
                    continue;
                }

                // (1) Tạo IdentityUser
                var user = new ApplicationUser
                {
                    UserName = code,
                    Email = $"{code}@marixa.com",
                    EmailConfirmed = true,
                    PhoneNumber = code,
                    CreatedBy = "Seeder"
                };

                var createResult = await userManager.CreateAsync(user, Password);
                if (!createResult.Succeeded)
                {
                    logger.LogWarning(
                        "SampleDataSeeder: FAILED to create user {Code}: {Errors}",
                        code, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }

                // (2) Gán role
                if (await roleManager.RoleExistsAsync(role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }

                // (3) Tạo Employee (tài khoản nhân viên)
                var employee = new Employee
                {
                    UserId = user.Id,
                    EmployeeCode = code,
                    GivenName = givenName,
                    FamilyName = familyName,
                    Email = user.Email,
                    PhoneNumber = code,
                    Status = EmployeeStatus.Working,
                    LaborType = LaborType.FullTime,
                    StartDate = new DateTime(2024, 1, 1),
                    CreatedBy = "Seeder"
                };

                db.Employees.Add(employee);
                await db.SaveChangesAsync();

                // (4) Đồng bộ 2 chiều: user.EmployeeId + employee.UserId
                user.EmployeeId = employee.Id;
                await userManager.UpdateAsync(user);

                logger.LogInformation("SampleDataSeeder: created {Code} ({GivenName} {FamilyName}, role={Role})",
                    code, givenName, familyName, role);
            }

            logger.LogInformation("SampleDataSeeder: done ({Count} users defined)", Users.Length);
        }
    }
}
