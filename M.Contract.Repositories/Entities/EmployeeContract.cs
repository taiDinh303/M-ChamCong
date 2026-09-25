using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class EmployeeContract : BaseEntity
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContractNumber { get; set; } = string.Empty;

        [Required]
        public ContractType ContractType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? Note { get; set; }
    }

    public enum ContractType
    {
        Probation = 1,
        FixedTerm = 2,
        IndefiniteTerm = 3,
        Seasonal = 4
    }
}