using M.Contract.Repositories.Entities;
using M.Core.Utils;
using Microsoft.AspNetCore.Identity;

namespace M.Contract.Repositories.Entity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        // =========================================================
        // THÔNG TIN NHÂN VIÊN
        // =========================================================

        public virtual Employee? Employee { get; set; }


        // =========================================================
        // AUDIT
        // =========================================================

        public string? CreatedBy { get; set; }

        public string? LastUpdatedBy { get; set; }

        public string? DeletedBy { get; set; }

        public DateTimeOffset CreatedTime { get; set; }

        public DateTimeOffset LastUpdatedTime { get; set; }

        public DateTimeOffset? DeletedTime { get; set; }


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ApplicationUser()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}