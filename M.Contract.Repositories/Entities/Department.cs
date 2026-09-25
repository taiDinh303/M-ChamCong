using M.Core.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace M.Contract.Repositories.Entities
{
    public class Department : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid? ManagerId { get; set; }

        [ForeignKey(nameof(ManagerId))]
        public virtual Employee? Manager { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Employee> Employees { get; set; }
            = new List<Employee>();
    }
}