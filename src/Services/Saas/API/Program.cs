using Microsoft.EntityFrameworkCore;
using _360Retail.Shared.Common.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using _360Retail.Services.Saas.Infrastructure.Persistence;
using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.Infrastructure.Services;
using _360Retail.Services.Saas.API.Services;
using Microsoft.OpenApi.Models;
using _360Retail.Services.Saas.Infrastructure.HttpClients;
using _360Retail.Shared.Email;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===== SERILOG =====
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.WithProperty("Service", "SaaS")
    .WriteTo.Console());

// Controllers
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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "360Retail Saas API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập JWT token dạng: Bearer {your_token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


// DbContext
builder.Services.AddDbContext<SaasDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("SaasDb")
    )
);

// DI
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton<VNPayService>();
builder.Services.AddSingleton<SePayService>();
builder.Services.AddScoped<IPlanReviewService, PlanReviewService>();
builder.Services.AddSingleton<ChatbotService>();

// Shared Email Services (for subscription expiry notifications)
builder.Services.AddSharedEmailServices();

// HTTP Client -> Identity Service
var identityServiceUrl = builder.Configuration["ServiceUrls:IdentityService"] 
    ?? "http://localhost:5297";
builder.Services.AddHttpClient<IIdentityClient, IdentityClient>(client =>
{
    client.BaseAddress = new Uri(identityServiceUrl);
});

// Authentication (JWT)
var jwtSection = builder.Configuration.GetSection("JwtSettings");

var key = jwtSection["Key"];
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];

if (string.IsNullOrWhiteSpace(key))
{
    throw new Exception("JWT Key is missing in Saas.API appsettings.json");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key)
            ),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Serilog request logging - must be first to see final status codes
app.UseSerilogRequestLogging();

// Global Exception Handler - converts exceptions to proper HTTP responses
app.UseGlobalExceptionHandler();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
