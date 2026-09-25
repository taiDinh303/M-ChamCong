using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class AttendanceLog : BaseEntity
    {
        // Khóa ngoại tới bản ghi Attendance
        [Required]
        public Guid AttendanceId { get; set; }

        // Điều hướng tới Attendance
        [ForeignKey(nameof(AttendanceId))]
        public virtual Attendance? Attendance { get; set; }

        // Thời gian log (với timezone)
        [Required]
        public DateTimeOffset LogTime { get; set; }

        // Loại log: vào / ra
        [Required]
        public AttendanceLogType Type { get; set; }

        // Phương thức chấm công (điện thoại, vân tay, GPS...)
        public AttendanceMethod Method { get; set; }
        // Tọa độ (nếu có) lưu ở dạng decimal với độ chính xác
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        // Mã thiết bị (nếu có)
        [MaxLength(100)]
        public string? DeviceId { get; set; }

        // Ghi chú thêm
        public string? Note { get; set; }
    }

    // Kiểu log: CheckIn hoặc CheckOut
    public enum AttendanceLogType
    {
        CheckIn = 1,
        CheckOut = 2
    }

    // Các phương thức thực hiện chấm công
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
