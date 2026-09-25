using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class EmployeeBankAccount : BaseEntity
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Required]
        public Guid BankId { get; set; }

        [ForeignKey(nameof(BankId))]
        public virtual Bank? Bank { get; set; }

        [Required]
        [MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? AccountHolderName { get; set; }

        public bool IsPrimary { get; set; } = false;

        public int Status { get; set; } = 1;
    }
}