using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.API.Hubs;
using _360Retail.Shared.Common.Middleware;
using _360Retail.Shared.Email;
using _360Retail.Services.Identity.Application.Interfaces.SuperAdmin;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using _360Retail.Services.Identity.Infrastructure.Services;
using _360Retail.Services.Identity.Infrastructure.Services.Email;
using _360Retail.Services.Identity.Infrastructure.Services.Invitations;
using _360Retail.Services.Identity.Infrastructure.Services.SuperAdmin;
using _360Retail.Services.Identity.Infrastructure.Services.UserStoreAccess;
using _360Retail.Services.Identity.API.Services;
using _360Retail.Services.Saas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ===== SERILOG =====
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.WithProperty("Service", "Identity")
    .WriteTo.Console());

#region ===== HTTP & EMAIL =====
builder.Services.AddHttpClient();

// Named HttpClient for HR Service
builder.Services.AddHttpClient("HrService", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:HrService"] ?? "http://localhost:5280";
    client.BaseAddress = new Uri(baseUrl);

    // Add internal API key for cross-service authentication
    var internalKey = builder.Configuration["InternalApi:Key"] ?? "360retail-internal-secret-key";
    client.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
});

// Named HttpClient for SaaS Service (for trial store creation)
builder.Services.AddHttpClient("SaasService", client =>
{
    // Use service name 'saas-api' for Docker internal communication
    // Fallback to localhost for local dev without Docker
    var baseUrl = builder.Configuration["ServiceUrls:SaasService"] ?? "http://saas-api:8080"; 
    client.BaseAddress = new Uri(baseUrl);

    // Add internal API key for cross-service authentication
    var internalKey = builder.Configuration["InternalApi:Key"] ?? "360retail-internal-secret-key";
    client.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
});

builder.Services.AddSharedEmailServices();
builder.Services.AddScoped<IEmailService, ResendEmailService>();
#endregion

#region ===== DATABASE =====
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<SaasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region ===== REDIS CACHE =====
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = "360Retail_Identity_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSingleton<TokenBlacklistService>();
#endregion

#region ===== APPLICATION SERVICES =====

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ISuperAdminUserService, SuperAdminUserService>();

builder.Services.AddScoped<IUserInvitationService, UserInvitationService>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IUserStoreAccessService, UserStoreAccessService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ===== SIGNALR =====
builder.Services.AddSignalR();

#endregion

#region ===== JWT AUTHENTICATION =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero
    };

    // Support SignalR: read JWT from query string for WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notifications/hub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});
#endregion

#region ===== SWAGGER =====
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "360Retail Identity API",
        Version = "v1"
    });

    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Paste JWT token here (no need to type Bearer)",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});
#endregion

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

// Add CORS - read origins from config (overridable via env var)
var corsOrigins = builder.Configuration["CorsOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
    ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

#region ===== MIDDLEWARE =====
// Serilog request logging - must be first to see final status codes
app.UseSerilogRequestLogging();

// Global Exception Handler - converts exceptions to proper HTTP responses
app.UseGlobalExceptionHandler();

// Protect internal APIs with shared key
app.UseInternalApiKeyProtection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Auto redirect / to /swagger
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseMiddleware<_360Retail.Services.Identity.API.Middleware.TokenBlacklistMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notifications/hub");
app.MapHealthChecks("/health");
#endregion

app.Run();
