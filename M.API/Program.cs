using M.API;
using M.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();