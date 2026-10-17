using M.Contract.Services.Interface;
using M.ModelViews.AttendancePeriodModelView;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace M.API.Controllers.Timekeeping;

[ApiController, Authorize, Route("api/AttendancePeriod/{year:int}/{month:int}")]
public sealed class AttendancePeriodController(IAttendancePeriodService service) : BaseApiController
{
    [HttpGet, Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> Get(int year, int month) => Success(await service.GetAsync(year, month));
    [HttpGet("history"), Authorize(Roles = "Admin,Manager,HR")]
    public async Task<IActionResult> History(int year, int month) => Success(await service.HistoryAsync(year, month));
    [HttpPost("review"), Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Review(int year, int month, PeriodActionRequest request) => Success(await service.ReviewAsync(year, month, request.Reason));
    [HttpPost("lock"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Lock(int year, int month, PeriodActionRequest request) => Success(await service.LockAsync(year, month, request.Reason));
    [HttpPost("reopen"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reopen(int year, int month, PeriodActionRequest request) => Success(await service.ReopenAsync(year, month, request.Reason));
    [HttpGet("snapshot")]
    public async Task<IActionResult> Snapshot(int year, int month, int? version) => Success(await service.SnapshotAsync(year, month, version));
    [HttpGet("export.xlsx")]
    public async Task<IActionResult> Export(int year, int month, int? version)
    {
        var document = await service.ExportAsync(year, month, version);
        Response.Headers.CacheControl = "no-store";
        return File(document.Content, document.ContentType, document.FileName);
    }
}
