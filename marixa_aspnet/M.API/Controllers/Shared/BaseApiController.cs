using System.Security.Claims;
using M.Core.Base;
using M.Core.Security;
using M.Core.Store;
using Microsoft.AspNetCore.Mvc;

namespace M.API.Controllers;

/// <summary>Transport helpers only. Authorization and business rules remain explicit.</summary>
public abstract class BaseApiController : ControllerBase
{
    protected Guid? ActorUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) ? id : null;
    protected Guid? ActorEmployeeId => Guid.TryParse(User.FindFirstValue("employeeId"), out var id) ? id : null;
    protected bool ActorIsStaff => User.IsInRole(RoleNames.Admin)
        || User.IsInRole(RoleNames.HR) || User.IsInRole(RoleNames.Manager);

    protected OkObjectResult Success<T>(T data, string? message = null) =>
        Ok(new BaseResponse<T>(StatusCodeHelper.OK, ResponseCodeConstants.SUCCESS, data, message));
}
