using M.ModelViews.Files;
using System.ComponentModel.DataAnnotations;

namespace M.ModelViews.HumanResources;

public class ReportSubmission
{
    public Guid? ReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string? Deadline { get; set; }
    public UploadedDocument? File { get; set; }
    public List<UploadedDocument>? Files { get; set; }
    public string? Overview { get; set; }
    public string? Results { get; set; }
    public string? Issues { get; set; }
    public string? Recommendations { get; set; }
}

public class ReviewForm { public int Status { get; set; } public string? Comment { get; set; } }
public class ForwardForm { public string Recipient { get; set; } = string.Empty; public Guid? RecipientEmployeeId { get; set; } public string? Note { get; set; } }
public class UpperRequestForm { public int Status { get; set; } public string Request { get; set; } = string.Empty; public DateTime? Deadline { get; set; } }
public class RespondForm { public string Comment { get; set; } = string.Empty; public bool TransferToEmployee { get; set; } }
