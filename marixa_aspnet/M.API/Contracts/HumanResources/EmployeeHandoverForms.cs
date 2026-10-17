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
    public M.ModelViews.HumanResources.HandoverSubmission ToSubmission() => new()
    {
        LastWorkingDate = LastWorkingDate, Reason = Reason, AssetsJson = AssetsJson,
        ProjectsJson = ProjectsJson, AttachmentMetadataJson = AttachmentMetadataJson,
        Attachments = Attachments.Select(file => new M.ModelViews.Files.UploadedDocument(file.FileName, file.Length, file.OpenReadStream)).ToList()
    };
}
