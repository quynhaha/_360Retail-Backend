using _360Retail.Services.HR.Application.Interfaces;
using _360Retail.Shared.Common.Middleware;
using _360Retail.Shared.Email;
using _360Retail.Services.HR.Infrastructure.Persistence;
using _360Retail.Services.HR.Infrastructure.Services;
using _360Retail.Services.HR.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    .Enrich.WithProperty("Service", "HR")
    .WriteTo.Console());

#region ===== DATABASE =====
builder.Services.AddDbContext<HrDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region ===== HTTP CLIENT FOR IDENTITY SERVICE =====
builder.Services.AddHttpClient("IdentityService", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:IdentityService"] ?? "http://localhost:5297";
    client.BaseAddress = new Uri(baseUrl);

    // Add internal API key for cross-service authentication
    var internalKey = builder.Configuration["InternalApi:Key"] ?? "360retail-internal-secret-key";
    client.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
});
#endregion

#region ===== APPLICATION SERVICES =====
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITimekeepingService, TimekeepingService>();
builder.Services.AddSharedEmailServices();
builder.Services.AddScoped<IEmailService, ResendEmailService>();
#endregion

#region ===== JWT AUTHENTICATION =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!");

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
        ValidIssuer = jwtSettings["Issuer"] ?? "360Retail",
        ValidAudience = jwtSettings["Audience"] ?? "360RetailUsers",
        ClockSkew = TimeSpan.Zero
    };
});
#endregion

#region ===== SWAGGER =====
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "360Retail HR API",
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:4200")
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
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
#endregion

app.Run();
