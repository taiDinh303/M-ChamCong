using M.Core.Utils;
using Microsoft.AspNetCore.Identity;

namespace M.Contract.Repositories.Entities
{
    public class ApplicationUserRole : IdentityUserRole<Guid>
    {
        public string? CreatedBy { get; set; }
        public string? LastUpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
        public DateTimeOffset? DeletedTime { get; set; }

        public ApplicationUserRole()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}
