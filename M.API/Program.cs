using M.API;
using M.API.Middleware;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Cho phép upload ảnh chấm công tối đa 5MB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024 * 1024;
});

var jwtKey = builder.Configuration["JwtSettings:Key"]
    ?? throw new InvalidOperationException("JWT Key not found");

builder.Services.AddEndpointsApiExplorer();

// Add all services to this
builder.Services.AddConfig(builder.Configuration);


var app = builder.Build();

//Catch error
//app.UseDeveloperExceptionPage();
app.UseMiddleware<ExceptionMiddleware>();



app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("ReactPolicy");

// Ảnh chấm công đã upload -> /uploads
var uploadDir = Path.Combine(AppContext.BaseDirectory, "uploads");
Directory.CreateDirectory(uploadDir);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadDir),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
