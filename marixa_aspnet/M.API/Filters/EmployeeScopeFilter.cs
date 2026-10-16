using M.Core.Security;
using Microsoft.AspNetCore.Mvc.Filters;
using static M.Core.Base.BaseException;

namespace M.API.Filters;

/// <summary>Defense against payload reassignment; services must also guard resources loaded by ID.</summary>
public sealed class EmployeeScopeFilter(ICurrentUser actor) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (actor.IsAuthenticated && !actor.IsInRole(RoleNames.Admin)
            && !actor.IsInRole(RoleNames.HR) && !actor.IsInRole(RoleNames.Manager))
        {
            foreach (var (name, value) in context.ActionArguments)
            {
                var employee = name.Equals("employeeId", StringComparison.OrdinalIgnoreCase) ? value
                    : value?.GetType().GetProperty("EmployeeId")?.GetValue(value);
                if (employee is Guid id && id != actor.EmployeeId)
                    throw new ErrorException(403, "FORBIDDEN", "Employee data is outside your access scope.");
            }
        }
        await next();
    }
}
