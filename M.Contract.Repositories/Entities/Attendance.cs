using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class Attendance : BaseEntity
    {
        // Khóa ngoại tới nhân viên (bắt buộc)
        [Required]
        public Guid EmployeeId { get; set; }

        // Điều hướng tới đối tượng Employee
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        // Ngày chấm công (bắt buộc)
        [Required]
        public DateTime AttendanceDate { get; set; }

        // Trạng thái chấm công (có thể null nếu chưa xác định)
        public AttendanceStatus? Status { get; set; }

        // Ghi chú thêm
        public string? Note { get; set; }

        // Danh sách các bản ghi log chấm công liên quan
        public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; }
            = new List<AttendanceLog>();
    }

    // Các trạng thái chấm công được hỗ trợ
    public enum AttendanceStatus
    {
        Present = 1,
        Late = 2,
        EarlyLeave = 3,
        Absent = 4,
        Leave = 5,
        Holiday = 6,
        Weekend = 7
    }
}
