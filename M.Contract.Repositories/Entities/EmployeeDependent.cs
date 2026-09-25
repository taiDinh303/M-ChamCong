using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class EmployeeDependent : BaseEntity
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Required]
        [MaxLength(100)]
        public string GivenName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FamilyName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{GivenName} {FamilyName}".Trim();

        [MaxLength(50)]
        public string? Relationship { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(20)]
        public string? CitizenId { get; set; }

        [MaxLength(50)]
        public string? TaxIdentificationNumber { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public int Status { get; set; } = 1;
    }
}