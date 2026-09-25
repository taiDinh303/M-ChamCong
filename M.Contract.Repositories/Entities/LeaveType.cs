using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class LeaveType : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxDays { get; set; }

        public bool IsPaid { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; }
            = new List<LeaveRequest>();
    }
}