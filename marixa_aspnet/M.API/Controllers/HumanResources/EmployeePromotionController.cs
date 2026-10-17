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
public sealed class EmployeePromotionController(IEmployeePromotionService service) : BaseApiController
{
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

    [HttpGet("options")]
    public async Task<IActionResult> Options()
    {
        return Success(await service.Options());
    }

    [HttpPost("request")]
    public async Task<IActionResult> CreateRequest(PromotionRequest form)
    {
        return Success(await service.CreateRequest(form));
    }

    [HttpPost("review/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> Review(Guid id, PromotionReview form)
    {
        return Success(await service.Review(id, form));
    }

}
