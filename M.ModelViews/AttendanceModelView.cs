using M.Contract.Repositories.Entities;
using System.ComponentModel.DataAnnotations;

namespace ModelViews.AttendanceModelView
{
    public class AttendanceResponseModelView
    {
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        public string? EmployeeCode { get; set; }

        public string? EmployeeName { get; set; }

        public DateTime AttendanceDate { get; set; }

        public AttendanceStatus? Status { get; set; }

        // Kế hoạch
        public Guid? PlannedShiftId { get; set; }
        public string? PlannedShiftName { get; set; }
        public int? PlannedHours { get; set; }
        public int? ActualHours { get; set; }

        // Ảnh
        public string? CheckInPhoto { get; set; }
        public string? CheckOutPhoto { get; set; }

        // Phê duyệt
        public AttendanceApprovalStatus ApprovalStatus { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? ApproverName { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public string? Note { get; set; }

        public DateTimeOffset CreatedTime { get; set; }

        public DateTimeOffset LastUpdatedTime { get; set; }
    }


    public class CreateAttendanceModelView
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        public AttendanceStatus? Status { get; set; }

        // Kế hoạch (tùy chọn khi tạo)
        public Guid? PlannedShiftId { get; set; }
        public int? PlannedHours { get; set; }

        public string? Note { get; set; }
    }


    public class UpdateAttendanceModelView
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid EmployeeId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        public AttendanceStatus? Status { get; set; }

        public Guid? PlannedShiftId { get; set; }
        public int? PlannedHours { get; set; }
        public int? ActualHours { get; set; }

        public string? CheckInPhoto { get; set; }
        public string? CheckOutPhoto { get; set; }

        public AttendanceApprovalStatus ApprovalStatus { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public string? Note { get; set; }
    }

    // Bản ghi phê duyệt nhanh (approve / reject)
    public class ApproveAttendanceModelView
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public AttendanceApprovalStatus ApprovalStatus { get; set; }

        [Required]
        public Guid ApprovedBy { get; set; }

        public string? Note { get; set; }
    }
}
