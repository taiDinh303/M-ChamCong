using M.Core.Utils;
using System.ComponentModel.DataAnnotations;

namespace M.Core.Base
{
    public abstract class BaseEntity
    {
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedTime = LastUpdatedTime = CoreHelper.SystemTimeNow;
        }

        [Key]
        public Guid Id { get; set; }
        public string? CreatedBy { get; set; }
        public string? LastUpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
        public DateTimeOffset? DeletedTime { get; set; }

        public void MarkUpdated(string actor, DateTimeOffset at)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actor);
            LastUpdatedBy = actor;
            LastUpdatedTime = at.ToUniversalTime();
        }

        public void SoftDelete(string actor, DateTimeOffset at)
        {
            if (DeletedTime.HasValue) return;
            MarkUpdated(actor, at);
            DeletedBy = actor;
            DeletedTime = at.ToUniversalTime();
        }

        public void Restore(string actor, DateTimeOffset at)
        {
            if (!DeletedTime.HasValue) return;
            MarkUpdated(actor, at);
            DeletedTime = null;
            DeletedBy = null;
        }
    }
}

