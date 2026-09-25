using M.Core.Base;
using System.ComponentModel.DataAnnotations;

namespace M.Contract.Repositories.Entities
{
    public class Bank : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ShortName { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<EmployeeBankAccount> EmployeeBankAccounts { get; set; }
            = new List<EmployeeBankAccount>();
    }
}