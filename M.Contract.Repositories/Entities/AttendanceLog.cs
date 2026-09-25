using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class AttendanceLog : BaseEntity
    {
        [Required]
        public Guid AttendanceId { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public virtual Attendance? Attendance { get; set; }

        [Required]
        public DateTimeOffset LogTime { get; set; }

        [Required]
        public AttendanceLogType Type { get; set; }

        public AttendanceMethod Method { get; set; }
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        [MaxLength(100)]
        public string? DeviceId { get; set; }

        public string? Note { get; set; }
    }

    public enum AttendanceLogType
    {
        CheckIn = 1,
        CheckOut = 2
    }

    public enum AttendanceMethod
    {
        Manual = 1,
        Fingerprint = 2,
        FaceRecognition = 3,
        Phone = 4,
        GPS = 5,
        Import = 6
    }
}