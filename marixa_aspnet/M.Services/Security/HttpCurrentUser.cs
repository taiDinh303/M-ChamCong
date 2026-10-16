using System.Security.Claims;
using M.Core.Security;
using Microsoft.AspNetCore.Http;

namespace M.Services.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public Guid? UserId => ParseClaim(ClaimTypes.NameIdentifier) ?? ParseClaim("sub");
    public Guid? EmployeeId => ParseClaim("employeeId");
    public string Name => Principal?.Identity?.Name ?? "System";
    public bool IsInRole(string role) => IsAuthenticated && Principal!.IsInRole(role);

    private Guid? ParseClaim(string name) =>
        Guid.TryParse(Principal?.FindFirst(name)?.Value, out var id) && id != Guid.Empty ? id : null;
}
