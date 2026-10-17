using System.ComponentModel.DataAnnotations;

namespace M.API.Contracts.HumanResources;

public class ReportSubmitForm
{
    public Guid? ReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string? Deadline { get; set; }
    public IFormFile? File { get; set; }
    public List<IFormFile>? Files { get; set; }
    public string? Overview { get; set; }
    public string? Results { get; set; }
    public string? Issues { get; set; }
    public string? Recommendations { get; set; }
    public M.ModelViews.HumanResources.ReportSubmission ToSubmission() => new()
    {
        ReportId = ReportId, Title = Title, ReportType = ReportType, Period = Period, Deadline = Deadline,
        Overview = Overview, Results = Results, Issues = Issues, Recommendations = Recommendations,
        File = File == null ? null : new(File.FileName, File.Length, File.OpenReadStream),
        Files = Files?.Select(file => new M.ModelViews.Files.UploadedDocument(file.FileName, file.Length, file.OpenReadStream)).ToList()
    };
}
