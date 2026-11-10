using M.Core.Base;
using M.Repositories.Context;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace M.API.Filters;

// Request metadata is validated before the service runs. Audit snapshots are
// inserted by persistence in the same SaveChanges transaction as the mutation.
public sealed class WorkforceEditAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (request.Method is "GET" or "HEAD") { await next(); return; }
        var reason = Uri.UnescapeDataString(request.Headers["X-Change-Reason"].ToString()).Trim();
        if (reason.Length is < 5 or > 500)
            throw new BaseException.ErrorException(400, "CHANGE_REASON_REQUIRED", "Provide a change reason of 5–500 characters.");
        var db = context.HttpContext.RequestServices.GetRequiredService<DatabaseContext>();
        db.WorkforceChangeReason = reason;
        foreach (var value in context.ActionArguments.Values.Where(x => x != null))
            foreach (var property in value!.GetType().GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite))
            {
                if (property.GetValue(value) is not string text) continue;
                property.SetValue(value, text.Trim());
                if (property.Name is "Code" or "ContractNumber" or "Name" && string.IsNullOrWhiteSpace(text))
                    throw new BaseException.ErrorException(400, "INVALID_INPUT", "Required text cannot be blank.");
            }
        var controller = context.RouteData.Values["controller"]?.ToString();
        var type = db.Model.GetEntityTypes().Single(t => t.ClrType.Name == controller).ClrType;
        var model = context.ActionArguments.Values.FirstOrDefault(v => v?.GetType().GetProperty("Id") != null);
        var id = model?.GetType().GetProperty("Id")?.GetValue(model) as Guid?
            ?? context.ActionArguments.Values.OfType<Guid>().Cast<Guid?>().FirstOrDefault();
        if (id.HasValue)
        {
            if (!DateTimeOffset.TryParse(request.Headers["X-Record-Version"], out var version))
                throw new BaseException.ErrorException(428, "RECORD_VERSION_REQUIRED", "Reload the record before editing.");
            var entity = await db.FindAsync(type, id.Value);
            if (entity == null) throw new BaseException.ErrorException(404, "NOT_FOUND", "Record not found.");
            var original = (DateTimeOffset)db.Entry(entity).Property("LastUpdatedTime").CurrentValue!;
            if (original != version)
                throw new BaseException.ErrorException(409, "CONCURRENT_CHANGE", "This record changed. Reload before editing.");
        }
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var result = await next();
        if (result.Exception == null && (context.HttpContext.Response.StatusCode < 400) && result.Result is not Microsoft.AspNetCore.Mvc.ObjectResult { StatusCode: >= 400 })
            await transaction.CommitAsync();
    }
}
