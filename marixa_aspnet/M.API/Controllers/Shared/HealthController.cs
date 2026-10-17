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
            await db.AttendancePeriods.AsNoTracking().AnyAsync(cancellationToken);
            await db.AttendanceRules.AsNoTracking().Select(r => r.BreakStartTime).Take(1).ToListAsync(cancellationToken);
            await db.EmployeeHandovers.AsNoTracking().Select(h => h.AppliedAt).Take(1).ToListAsync(cancellationToken);
            await db.LeaveRequests.AsNoTracking().Select(r => r.CancelledAt).Take(1).ToListAsync(cancellationToken);
            return Success(new { status = "ready" });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return StatusCode(503, new { statusCode = 503, code = "NOT_READY", message = "Database unavailable.", data = (object?)null });
        }
    }
}
