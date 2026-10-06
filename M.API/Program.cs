using M.API;
using M.API.Middleware;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// File bao cao toi da 20MB (upload anh van duoc gioi han riêng trong API cham cong).
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 21 * 1024 * 1024;
});

var jwtKey = builder.Configuration["JwtSettings:Key"]
    ?? throw new InvalidOperationException("JWT Key not found");

builder.Services.AddEndpointsApiExplorer();

// Add all services to this
builder.Services.AddConfig(builder.Configuration);


var app = builder.Build();

// Seed default roles - BOC try/catch: loi DB luc startup khong duoc giat app
// (neu crash o day, khong co middleware nao bat duoc -> IIS tra 500 xam).
try
{
    await M.API.Seed.RoleSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILogger>().LogWarning(
        "RoleSeeder skipped at startup: {Reason}", ex.Message);
}

//Catch error
//app.UseDeveloperExceptionPage();
app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ReactPolicy");
app.UseHttpsRedirection();

// Ảnh chấm công đã upload -> /uploads (lưu trong wwwroot/uploads, ổn định)
var uploadDir = PhotoStore.GetRoot(app.Environment);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadDir),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SPA fallback (chi chay IIS, khong dung URL Rewrite module - vi shared hosting
// thieu module nay, dung rewrite se gay "URL Rewrite Module Error" -> 500 moi request).
// Route frontend (React Router) khi hard-refresh (VD /employees) -> index.html;
// /api, /uploads, /swagger khong phu hop -> 404.
string webRoot = app.Environment.WebRootPath ?? "";
string indexFile = System.IO.Path.Combine(webRoot, "index.html");
app.MapFallback(async context =>
{
    string p = context.Request.Path.Value ?? "";
    if (p.StartsWith("/api") || p.StartsWith("/uploads") || p.StartsWith("/swagger"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    if (System.IO.File.Exists(indexFile))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(indexFile, context.RequestAborted);
        return;
    }
    context.Response.StatusCode = StatusCodes.Status404NotFound;
});

app.Run();
