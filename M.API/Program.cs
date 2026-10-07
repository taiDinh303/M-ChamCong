using M.API;
using M.API.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// File báo cáo tối đa 20MB (upload ảnh vẫn được giới hạn riêng trong API chấm công).
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

// Dev: tu dong migrate CSDL local de khua schema moi (AttendanceLogs, ChangeSummary,
// EmployeeReports, EmployeePromotions, EmployeeHandovers...). Idempotent + chi chay
// Development (khong dong CSDL Somee trong Production). Khua loi 500 "invalid column/table".
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        await scope.ServiceProvider
            .GetRequiredService<M.Repositories.Context.DatabaseContext>()
            .Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Services.GetRequiredService<ILogger>().LogWarning(
            "Auto-migrate skipped at startup: {Reason}", ex.Message);
    }
}

await M.API.Seed.RoleSeeder.SeedAsync(app.Services);

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

app.Run();
