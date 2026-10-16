using System.ComponentModel.DataAnnotations;

namespace M.API.Contracts.HumanResources;

public class HandoverRequest
{
    public DateTime LastWorkingDate { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(2000)] public string Reason { get; set; } = string.Empty;
    public string AssetsJson { get; set; } = "[]";
    public string ProjectsJson { get; set; } = "[]";
    public string AttachmentMetadataJson { get; set; } = "[]";
    public List<IFormFile> Attachments { get; set; } = [];
}

public class HandoverAssetRow
{
    public string AssetType { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class HandoverProjectRow
{
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Partner { get; set; } = string.Empty;
    public int Progress { get; set; }
    public List<HandoverFileInfo> Documents { get; set; } = [];
    public List<HandoverFileInfo> HandoverFiles { get; set; } = [];
}

public class HandoverAttachmentInput
{
    public int ProjectIndex { get; set; }
    public string Kind { get; set; } = string.Empty;
}

public class HandoverFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

public class HandoverReview
{
    public bool Approve { get; set; }
    public string? Note { get; set; }
}

public class HandoverRow
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public Guid ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CompanyAssets { get; set; }
    public string WorkProgress { get; set; } = string.Empty;
    public string AssetsJson { get; set; } = "[]";
    public string ProjectsJson { get; set; } = "[]";
    public bool AccountIssued { get; set; }
    public int Status { get; set; }
    public string? ReviewNote { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
