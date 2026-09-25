using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class EmployeeInsurance : BaseEntity
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [MaxLength(50)]
        public string? SocialInsuranceNumber { get; set; }

        [MaxLength(50)]
        public string? HealthInsuranceNumber { get; set; }

        [MaxLength(50)]
        public string? PersonalTaxCode { get; set; }

        public bool IsSocialInsuranceParticipant { get; set; } = false;

        public DateTime? ParticipationStartDate { get; set; }

        public DateTime? ParticipationEndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SocialInsuranceSalary { get; set; } = 0;

        public int Status { get; set; } = 1;
    }
}