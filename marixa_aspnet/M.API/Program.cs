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
builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("ReactPolicy");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PhotoStore.GetRoot(app.Environment)), RequestPath = "/uploads"
});
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
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
