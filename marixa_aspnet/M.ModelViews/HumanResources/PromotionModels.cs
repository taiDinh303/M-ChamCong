using System.ComponentModel.DataAnnotations;

namespace M.ModelViews.HumanResources;

public class PromotionRow
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? DepartmentManagerId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateTime? StartDate { get; set; }
    public string CurrentPosition { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public bool CanReview { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string CurrentEmployeeCode { get; set; } = string.Empty;
    public string? CurrentPositionName { get; set; }
    public string? NewEmployeeCode { get; set; }
    public Guid NewPositionId { get; set; }
    public string NewPositionName { get; set; } = string.Empty;
    public string NewPositionCode { get; set; } = string.Empty;
    public string TargetRoleName { get; set; } = string.Empty;
    public string ProposedByName { get; set; } = string.Empty;
    public Guid? ProposedByEmployeeId { get; set; }
    public int ProposalType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalNote { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime EffectiveDate { get; set; }
    public int Status { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}

public class PromotionRequest
{
    public Guid? EmployeeId { get; set; }
    public Guid NewPositionId { get; set; }
    public string TargetRoleName { get; set; } = "Employee";
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalNote { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class PromotionReview
{
    public bool Approve { get; set; }
    public string? Comment { get; set; }
}
