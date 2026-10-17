using M.API;
using M.API.Bootstrap;
using M.API.Middleware;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<M.API.Filters.EmployeeScopeFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<M.API.Filters.DateTimeUtcKindFilter>();
    options.Filters.AddService<M.API.Filters.EmployeeScopeFilter>();
})
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(new
        {
            statusCode = 400, code = "VALIDATION", message = "Request validation failed.",
            data = context.ModelState.Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(x => x.Key, x => x.Value!.Errors.Select(e =>
                    string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray())
        }));
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 21 * 1024 * 1024);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddConfig(builder.Configuration, builder.Environment);
builder.Services.AddAuthRateLimits(builder.Configuration);
builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<EmploymentChangeWorker>();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();

app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("ReactPolicy");
// Legacy handover documents may still exist below uploads. All access goes through the guarded API.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads/handovers", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next(context);
});
app.UseDefaultFiles();
var uploads = PhotoStore.GetRoot(app.Environment);
var privateHandovers = Path.Combine(uploads, "handovers");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PublicFilesProvider(app.Environment.WebRootFileProvider, privateHandovers)
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PublicFilesProvider(new PhysicalFileProvider(uploads), privateHandovers),
    RequestPath = "/uploads"
});
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapFallback(async context =>
{
    var index = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/uploads")
        || context.Request.Path.StartsWithSegments("/swagger") || !File.Exists(index))
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsJsonAsync(new { statusCode = 404, code = "NOT_FOUND", message = "Route not found.", data = (object?)null });
        return;
    }
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(index);
}).AllowAnonymous();
app.Run();

// WebApplicationFactory entry point; does not change application behavior.
public partial class Program { }
