using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class LeaveRequest : BaseEntity
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Required]
        public Guid LeaveTypeId { get; set; }

        [ForeignKey(nameof(LeaveTypeId))]
        public virtual LeaveType? LeaveType { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalDays { get; set; }

        public string? Reason { get; set; }

        [Required]
        public LeaveRequestStatus Status { get; set; }
            = LeaveRequestStatus.Pending;

        public Guid? ApprovedBy { get; set; }

        [ForeignKey(nameof(ApprovedBy))]
        public virtual Employee? Approver { get; set; }

        public DateTime? ApprovedAt { get; set; }
    }

    public enum LeaveRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4
    }
}