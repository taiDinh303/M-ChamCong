using M.Contract.Repositories.Entities;
using System.ComponentModel.DataAnnotations;

namespace ModelViews.AttendanceLogModelView
{
    public class AttendanceLogResponseModelView
    {
        public Guid Id { get; set; }

        public Guid AttendanceId { get; set; }

        public Guid EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public DateTimeOffset LogTime { get; set; }

        public AttendanceLogType Type { get; set; }

        public AttendanceMethod Method { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string? DeviceId { get; set; }

        public string? Note { get; set; }

        public DateTimeOffset CreatedTime { get; set; }
    }


    public class CreateAttendanceLogModelView
    {
        [Required]
        public Guid AttendanceId { get; set; }

        [Required]
        public DateTimeOffset LogTime { get; set; }

        [Required]
        public AttendanceLogType Type { get; set; }

        [Required]
        public AttendanceMethod Method { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [MaxLength(100)]
        public string? DeviceId { get; set; }

        public string? Note { get; set; }
    }


    public class UpdateAttendanceLogModelView
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid AttendanceId { get; set; }

        [Required]
        public DateTimeOffset LogTime { get; set; }

        [Required]
        public AttendanceLogType Type { get; set; }

        [Required]
        public AttendanceMethod Method { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [MaxLength(100)]
        public string? DeviceId { get; set; }

        public string? Note { get; set; }
    }
}