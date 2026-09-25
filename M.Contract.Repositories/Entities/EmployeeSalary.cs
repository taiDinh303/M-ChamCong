using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class EmployeeSalary : BaseEntity
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        public Guid? SalaryGroupId { get; set; }

        [ForeignKey(nameof(SalaryGroupId))]
        public virtual SalaryGroup? SalaryGroup { get; set; }

        [Required]
        public SalaryPaymentType PaymentType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PositionAllowance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherAllowance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Bonus { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SocialInsuranceSalary { get; set; } = 0;

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }
    }

    public enum SalaryPaymentType
    {
        Monthly = 1,
        Daily = 2,
        Hourly = 3,
        Product = 4
    }
}