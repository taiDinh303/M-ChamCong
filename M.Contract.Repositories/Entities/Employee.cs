using M.Contract.Repositories.Entity;
using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class Employee : BaseEntity
    {
        // =========================================================
        // THÔNG TIN CƠ BẢN
        // =========================================================

        [Required]
        [MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string GivenName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FamilyName { get; set; } = string.Empty;
        [NotMapped]
        public string FullName => $"{GivenName} {FamilyName}".Trim();

        public DateTime? BirthDate { get; set; }

        public GenderType Gender { get; set; } = GenderType.RatherNotSay;


        // =========================================================
        // THÔNG TIN CCCD
        // =========================================================

        [MaxLength(20)]
        public string? CitizenId { get; set; }

        public DateTime? CitizenIdIssuedDate { get; set; }

        [MaxLength(255)]
        public string? CitizenIdIssuedPlace { get; set; }


        // =========================================================
        // THÔNG TIN LIÊN HỆ
        // =========================================================

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }


        // =========================================================
        // ĐỊA CHỈ
        // =========================================================

        public string? PermanentAddress { get; set; }

        public string? CurrentAddress { get; set; }


        // =========================================================
        // TÀI KHOẢN ĐĂNG NHẬP - OPTIONAL
        // =========================================================

        public Guid? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }


        // =========================================================
        // CÔNG VIỆC
        // =========================================================

        public Guid? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public virtual Department? Department { get; set; }


        public Guid? PositionId { get; set; }

        [ForeignKey(nameof(PositionId))]
        public virtual Position? Position { get; set; }


        // Quản lý trực tiếp
        public Guid? ManagerId { get; set; }

        [ForeignKey(nameof(ManagerId))]
        public virtual Employee? Manager { get; set; }


        // =========================================================
        // THỜI GIAN LÀM VIỆC
        // =========================================================

        public DateTime? StartDate { get; set; }

        public DateTime? ProbationEndDate { get; set; }


        // =========================================================
        // LOẠI NHÂN VIÊN / TRẠNG THÁI
        // =========================================================

        public LaborType LaborType { get; set; } = LaborType.FullTime;

        public EmployeeStatus Status { get; set; } = EmployeeStatus.Probation;


        // =========================================================
        // CHẤM CÔNG
        // =========================================================

        public bool UsePhoneAttendance { get; set; } = false;


        // =========================================================
        // GHI CHÚ
        // =========================================================

        public string? Note { get; set; }


        // =========================================================
        // QUAN HỆ
        // =========================================================

        // Nhân viên cấp dưới
        public virtual ICollection<Employee> Subordinates { get; set; }
            = new List<Employee>();

        // Hợp đồng
        public virtual ICollection<EmployeeContract> Contracts { get; set; }
            = new List<EmployeeContract>();

        // Lương
        public virtual ICollection<EmployeeSalary> Salaries { get; set; }
            = new List<EmployeeSalary>();

        // Bảo hiểm
        public virtual ICollection<EmployeeInsurance> Insurances { get; set; }
            = new List<EmployeeInsurance>();

        // Tài khoản ngân hàng
        public virtual ICollection<EmployeeBankAccount> BankAccounts { get; set; }
            = new List<EmployeeBankAccount>();

        // Người phụ thuộc
        public virtual ICollection<EmployeeDependent> Dependents { get; set; }
            = new List<EmployeeDependent>();

        // Chấm công
        public virtual ICollection<Attendance> Attendances { get; set; }
            = new List<Attendance>();

        // Nghỉ phép
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; }
            = new List<LeaveRequest>();

        // Bảng lương
        public virtual ICollection<Payroll> Payrolls { get; set; }
            = new List<Payroll>();
    }


    // =============================================================
    // ENUMS
    // =============================================================

    public enum LaborType
    {
        FullTime = 1,      // Nhân viên chính thức
        PartTime = 2,      // Nhân viên bán thời gian
        Intern = 3,        // Thực tập sinh
        Freelancer = 4     // Cộng tác viên / Freelancer
    }

    public enum EmployeeStatus
    {
        Probation = 1,     // Đang thử việc
        Working = 2,       // Đang làm
        OnLeave = 3,       // Tạm nghỉ
        Resigned = 4,      // Đã nghỉ việc
        Terminated = 5     // Chấm dứt hợp đồng
    }

    public enum GenderType
    {
        RatherNotSay = 0,
        Male = 1,
        Female = 2
    }
}
