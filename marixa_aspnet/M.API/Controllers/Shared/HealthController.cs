using M.Repositories.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace M.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("health")]
public sealed class HealthController(DatabaseContext db) : BaseApiController
{
    [HttpGet("live")]
    public IActionResult Live() => Success(new { status = "live" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            await db.Employees.AsNoTracking().AnyAsync(cancellationToken);
            return Success(new { status = "ready" });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return StatusCode(503, new { statusCode = 503, code = "NOT_READY", message = "Database unavailable.", data = (object?)null });
        }
    }
}
