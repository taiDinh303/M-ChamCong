using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using M.Repositories.Context;

namespace M.API.Seed
{
    /// <summary>
    /// Đảm bảo tài khoản quản trị (admin) tồn tại khi khởi động,
    /// idempotent: chạy lại nhiều lần không tạo trùng.
    ///



    ///
    /// Render có thể override qua env:
    /// </summary>
    public static class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            // UserManager / RoleManager / DatabaseContext là SCOPED ->
            // phải resolve trong scope, không resolve từ root provider.
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var config = sp.GetRequiredService<IConfiguration>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
            var db = sp.GetRequiredService<DatabaseContext>();

            string username = config["Seed:Admin:Username"] ?? "admin";
            string email = config["Seed:Admin:Email"] ?? "admin@monica.vn";
            string password = config["Seed:Admin:Password"]
                ?? throw new InvalidOperationException("Set Seed:Admin:Password before enabling admin bootstrap.");

            // Chờ role Admin tồn tại (RoleSeeder chạy trước)
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                return;
            }

            ApplicationUser? user =
                await userManager.FindByNameAsync(username)
                ?? await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    return;
                }
            }

            // Gắn role Admin (nếu chưa)
            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }

            // Gắn Employee (nếu chưa) để các màn hình cần EmployeeId chạy được
            Employee? employee =
                await db.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id)
                ?? await db.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == username);

            if (employee == null)
            {
                employee = new Employee
                {
                    UserId = user.Id,
                    EmployeeCode = username,
                    GivenName = username,
                    FamilyName = string.Empty,
                    Email = email,
                    Status = EmployeeStatus.Working,
                    LaborType = LaborType.FullTime,
                    StartDate = DateTime.UtcNow
                };

                db.Employees.Add(employee);
            }
            else if (employee.UserId != user.Id)
            {
                employee.UserId = user.Id;
            }

            await db.SaveChangesAsync();
        }
    }
}
