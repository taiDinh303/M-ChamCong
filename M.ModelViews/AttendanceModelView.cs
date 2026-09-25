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

        public string? Note { get; set; }
    }
}