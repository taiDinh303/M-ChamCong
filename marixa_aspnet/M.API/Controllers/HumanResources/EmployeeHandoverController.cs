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
public sealed class EmployeeHandoverController(IEmployeeHandoverService service) : BaseApiController
{
    private const long MaxRequestSize = 20 * 1024 * 1024;
    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        return Success(await service.Mine());
    }

    [HttpGet("get-all")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> GetAll()
    {
        return Success(await service.GetAll());
    }

    [HttpPost("request")]
    [RequestSizeLimit(MaxRequestSize + 1024 * 1024)]
    public async Task<IActionResult> SubmitRequest([FromForm] HandoverRequest form)
    {
        return Success(await service.SubmitRequest(form.ToSubmission()));
    }

    [HttpGet("{id:guid}/files/{storedName}")]
    public async Task<IActionResult> DownloadFile(Guid id, string storedName)
    {
        var document = await service.DownloadFile(id, storedName);
        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpPost("review/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> Review(Guid id, HandoverReview form)
    {
        return Success(await service.Review(id, form));
    }

}
