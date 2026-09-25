using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class Payroll : BaseEntity
    {
        // Khóa ngoại tới nhân viên
        [Required]
        public Guid EmployeeId { get; set; }

        // Điều hướng tới Employee
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        /// <summary>
        /// Ví dụ: 01/09/2026 đại diện cho kỳ lương tháng 09/2026.
        /// </summary>
        // Kỳ lương (thường lưu ngày bắt đầu tháng để đại diện tháng đó)
        [Required]
        public DateTime PayrollMonth { get; set; }

        // Các thành phần lương
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Allowance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Bonus { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Overtime { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Insurance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Deduction { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; } = 0;

        // Trạng thái bảng lương (Draft, Calculated, Approved...)
        [Required]
        public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
    }

    // Các trạng thái của bảng lương
    public enum PayrollStatus
    {
        Draft = 1,
        Calculated = 2,
        Approved = 3,
        Paid = 4,
        Cancelled = 5
    }
}
