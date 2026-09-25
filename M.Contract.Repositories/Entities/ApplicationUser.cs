using M.Core.Utils;
using Microsoft.AspNetCore.Identity;

namespace M.Contract.Repositories.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        // Ai tạo bản ghi
        public string? CreatedBy { get; set; }
        // Ai cập nhật lần cuối
        public string? LastUpdatedBy { get; set; }
        // Ai xóa
        public string? DeletedBy { get; set; }

        // Thời gian tạo
        public DateTimeOffset CreatedTime { get; set; }
        // Thời gian cập nhật lần cuối
        public DateTimeOffset LastUpdatedTime { get; set; }
        // Thời gian xóa (nếu có)
        public DateTimeOffset? DeletedTime { get; set; }

        // Liên kết tới thực thể Employee (nếu user là nhân viên)
        public virtual Employee? Employee { get; set; }

        // Khởi tạo thời gian mặc định khi tạo user
        public ApplicationUser()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}
