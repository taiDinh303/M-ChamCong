using M.API.Export;
using M.Contract.Services.Export;
using M.Core.Configuration;
using Microsoft.Extensions.Options;
using M.API.Storage;
using M.Contract.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Repositories.Context;
using M.Services;
using M.Repositories;
using M.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


namespace M.API.Configuration
{
    public static class DependencyInjection
    {

        public static void AddConfig(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.ConfigRoute();
            services.AddDatabase(configuration);
            services.AddIdentity();
            services.AddRepositories();
            services.AddBusinessServices();
            services.AddSingleton<IWorkforceExportWriter, WorkforceExportWriter>();
            services.AddJwtAuthentication(configuration, environment.IsProduction());
            services.AddSwaggerConfig();
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddAuthorization(options => options.FallbackPolicy =
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
            services.AddHttpClient<SupabasePhotoStorage>(client => client.Timeout = TimeSpan.FromSeconds(30));
            services.AddScoped<LocalPhotoStorage>();
            services.AddScoped<IPrivateDocumentStorage, LocalPrivateDocumentStorage>();
            services.AddScoped<IPhotoStorage>(sp => SupabasePhotoStorage.IsConfigured(configuration)
                ? sp.GetRequiredService<SupabasePhotoStorage>() : sp.GetRequiredService<LocalPhotoStorage>());

            services.AddCors(options =>
            {
                options.AddPolicy("ReactPolicy", policy =>
                {
                    policy
                        .SetIsOriginAllowed(origin =>
                            (environment.IsDevelopment() && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                                && uri.IsLoopback && uri.Scheme is "http" or "https")
                            || configuration.GetSection("Cors:AllowedOrigins").GetChildren()
                                .Any(v => string.Equals(v.Value, origin, StringComparison.OrdinalIgnoreCase)))
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders("X-Correlation-ID", "Retry-After", "Content-Disposition");
                });
            });
        }
        public static void ConfigRoute(this IServiceCollection services)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });
        }
        public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var dbConfig = DatabaseConfig.FromConfiguration(configuration);

            services.AddDbContext<DatabaseContext>(options =>
            {
                if (dbConfig.IsPostgres)
                {
                    options.UseNpgsql(dbConfig.ConnectionString);
                    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning));
                }
                else
                {
                    options.UseSqlServer(dbConfig.ConnectionString);
                    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning));
                }
            });
        }

        public static void AddIdentity(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 10;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = false;
            })
             .AddEntityFrameworkStores<DatabaseContext>()
             .AddDefaultTokenProviders();
        }

        //JWT
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, bool isProduction = false)
        {
            services.AddOptions<JwtSettings>().Bind(configuration.GetSection(JwtSettings.SectionName))
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.Key) && Encoding.UTF8.GetByteCount(settings.Key) >= 32
                    && (!isProduction || (!settings.Key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
                        && !settings.Key.Contains("DEVELOPMENT", StringComparison.OrdinalIgnoreCase))), "A private JWT signing key of at least 32 bytes is required.")
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer) && !string.IsNullOrWhiteSpace(settings.Audience),
                    "JWT issuer and audience are required.")
                .Validate(settings => settings.ExpirationMinutes is >= 1 and <= 10080, "Invalid JWT expiration.")
                .Validate(settings => settings.RememberMeExpirationMinutes >= settings.ExpirationMinutes
                    && settings.RememberMeExpirationMinutes <= 10080, "Remember-me expiration must be between the normal expiration and 7 days.")
                .ValidateOnStart();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>>((options, configured) =>
            {
                var settings = configured.Value;
                var key = Encoding.UTF8.GetBytes(settings.Key);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = settings.Audience,

                    ValidateLifetime = true,

                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuerSigningKey = true
                };


                // ==========================
                // Handle 401 / 403 Response
                // ==========================
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? context.Principal?.FindFirst("sub")?.Value;
                        var user = id is null ? null : await users.FindByIdAsync(id);
                        var stamp = context.Principal?.FindFirst("securityStamp")?.Value;
                        if (user is null || user.DeletedTime.HasValue || await users.IsLockedOutAsync(user)
                            || string.IsNullOrWhiteSpace(stamp) || stamp != user.SecurityStamp)
                        {
                            context.Fail("The session is no longer valid.");
                            return;
                        }
                        var employeeClaim = context.Principal?.FindFirst("employeeId")?.Value;
                        if (employeeClaim != null)
                        {
                            var db = context.HttpContext.RequestServices.GetRequiredService<DatabaseContext>();
                            if (!Guid.TryParse(employeeClaim, out var employeeId)
                                || !await db.Employees.AsNoTracking().AnyAsync(e => e.Id == employeeId
                                    && e.UserId == user.Id && !e.DeletedTime.HasValue, context.HttpContext.RequestAborted))
                            {
                                context.Fail("The employee link changed. Please sign in again.");
                                return;
                            }
                        }
                        var expected = await users.GetRolesAsync(user);
                        var actual = context.Principal!.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);
                        if (!expected.OrderBy(r => r).SequenceEqual(actual.OrderBy(r => r), StringComparer.OrdinalIgnoreCase))
                            context.Fail("Permissions changed. Please sign in again.");
                    },
                    // Reject missing or invalid tokens with a JSON challenge.
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";


                        var response = new
                        {
                            statusCode = 401,
                            code = ResponseCodeConstants.UNAUTHORIZED,
                            message = "You are not authenticated",
                            data = (object?)null
                        };


                        await context.Response.WriteAsJsonAsync(response);
                    },


                    // Authenticated actors still need the required role.
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";


                        var response = new
                        {
                            statusCode = 403,
                            code = ResponseCodeConstants.FORBIDDEN,
                            message = "You do not have permission to access this resource",
                            data = (object?)null
                        };


                        await context.Response.WriteAsJsonAsync(response);
                    }
                };
            });
        }
        //Author
        public static void AddSwaggerConfig(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

                // Expose calendar dates as OpenAPI date values.
                c.MapType<DateOnly>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "date",
                });

                // Bearer authorization for Swagger requests.
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your token"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }

        //public static void AddGoogleAuthentication(this IServiceCollection services, IConfiguration configuration)
        //{
        //    var clientId = configuration["Authentication:Google:ClientId"];
        //    var clientSecret = configuration["Authentication:Google:ClientSecret"];

        //    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        //        throw new InvalidOperationException("Google Authentication configuration is missing (ClientId or ClientSecret).");

        //    services.AddAuthentication()
        //        .AddGoogle(options =>
        //        {
        //            options.ClientId = clientId;
        //            options.ClientSecret = clientSecret;
        //        });
        //}




    }
}
