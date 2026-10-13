using M.Contract.Repositories.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace M.API.Seed
{
    /// <summary>
    /// Đảm bảo các role mặc định (Admin / Employee) tồn tại trong DB
    /// trước khi phân quyền [Authorize(Roles = "...")] được áp dụng.
    /// Chạy idempotent: chỉ tạo role khi chưa có.
    /// </summary>
    public static class RoleSeeder
    {
        // Core (đã map quyền module): Admin, Employee, Manager, HR.
        // Accounting/Sale/Marketing: bổ sung 12/10/2026 — "code trước",
        // chưa gán vào quyền module. Dùng tên "Accountant" (khớp SampleDataSeeder).
        private static readonly string[] Roles =
            { "Admin", "Employee", "Manager", "HR", "Accountant", "Sale", "Marketing" };

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            RoleManager<ApplicationRole> roleManager =
                scope.ServiceProvider.GetRequiredService<
                    RoleManager<ApplicationRole>>();

            foreach (string role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new ApplicationRole { Name = role });
                }
            }
        }
    }
}
