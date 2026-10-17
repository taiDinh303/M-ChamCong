using M.Contract.Services.Interface;
using M.Contract.Services.Storage;
using M.ModelViews.Files;
using M.ModelViews.HumanResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace M.API.Controllers.HumanResources;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class EmployeeReportController(IEmployeeReportService service) : BaseApiController
{
    private const long MaxFileSize = 20 * 1024 * 1024;
    [Authorize(Roles = "Admin,Manager,HR")]
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? period,
        [FromQuery] Guid? departmentId, [FromQuery] Guid? employeeId, [FromQuery] int? status,
        [FromQuery] string? reportType, [FromQuery] bool? overdue)
    {
        return Success(await service.GetAll(search, period, departmentId, employeeId, status, reportType, overdue));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        return Success(await service.Mine());
    }

    [HttpGet("by-employee/{employeeId:guid}")]
    public async Task<IActionResult> ByEmployee(Guid employeeId)
    {
        return Success(await service.ByEmployee(employeeId));
    }

    [HttpPost("submit")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> Submit([FromForm] ReportSubmitForm form)
    {
        return Success(await service.Submit(form.ToSubmission()));
    }

    [HttpPost("review/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewForm form)
    {
        return Success(await service.Review(id, form));
    }

    [HttpPost("forward/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> Forward(Guid id, [FromBody] ForwardForm form)
    {
        return Success(await service.Forward(id, form));
    }

    [HttpPost("upper-request/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> UpperRequest(Guid id, [FromBody] UpperRequestForm form)
    {
        return Success(await service.UpperRequest(id, form));
    }

    [HttpPost("upper-decision/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> UpperDecision(Guid id, [FromBody] UpperRequestForm form)
    {
        return Success(await service.UpperDecision(id, form));
    }

    [HttpPost("respond/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondForm form)
    {
        return Success(await service.Respond(id, form));
    }

    [HttpPost("complete/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> Complete(Guid id)
    {
        return Success(await service.Complete(id));
    }

    [HttpPost("viewed/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> Viewed(Guid id)
    {
        await service.Viewed(id);
        return NoContent();
    }

    [HttpPost("upper/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> UploadForUpper(Guid id, IFormFile? file, [FromForm] string? note, [FromForm] string? recipient)
    {
        return Success(await service.UploadForUpper(id, file == null ? null : new UploadedDocument(file.FileName, file.Length, file.OpenReadStream), note, recipient));
    }

    [HttpPost("mark-sent/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> MarkSent(Guid id)
    {
        return Success(await service.MarkSent(id));
    }

    [HttpGet("download/{versionId:guid}")]
    public async Task<IActionResult> Download(Guid versionId)
    {
        var document = await service.Download(versionId);
        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpGet("download-attachment/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
    {
        var document = await service.DownloadAttachment(attachmentId);
        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpGet("upper-download/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> DownloadUpper(Guid id)
    {
        var document = await service.DownloadUpper(id);
        return File(document.Content, document.ContentType, document.FileName);
    }

}
