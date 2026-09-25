using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class Attendance : BaseEntity
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        public AttendanceStatus? Status { get; set; }

        public string? Note { get; set; }

        public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; }
            = new List<AttendanceLog>();
    }

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