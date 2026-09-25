using M.Contract.Repositories.Entities;
using System.ComponentModel.DataAnnotations;

namespace ModelViews.LeaveRequestModelView
{
    public class LeaveRequestResponseModelView
    {
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public Guid LeaveTypeId { get; set; }

        public string? LeaveTypeName { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal? TotalDays { get; set; }

        public string? Reason { get; set; }

        public LeaveRequestStatus Status { get; set; }

        public Guid? ApprovedBy { get; set; }

        public string? ApproverName { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTimeOffset CreatedTime { get; set; }

        public DateTimeOffset LastUpdatedTime { get; set; }
    }


    public class CreateLeaveRequestModelView
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [Required]
        public Guid LeaveTypeId { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        public decimal? TotalDays { get; set; }

        public string? Reason { get; set; }
    }


    public class UpdateLeaveRequestModelView
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid EmployeeId { get; set; }

        [Required]
        public Guid LeaveTypeId { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        public decimal? TotalDays { get; set; }

        public string? Reason { get; set; }

        public LeaveRequestStatus Status { get; set; }

        public Guid? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }
    }
}