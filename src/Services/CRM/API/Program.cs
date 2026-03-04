using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using _360Retail.Services.CRM.Infrastructure.Persistence;
using _360Retail.Services.CRM.Infrastructure.Repositories;
using _360Retail.Services.CRM.Application.Services;
using _360Retail.Services.CRM.Application.Interfaces;
using _360Retail.Services.CRM.Application.Mappings;
using _360Retail.Services.CRM.API.Middleware;
using _360Retail.Services.CRM.API.Services;
using _360Retail.Shared.Common.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===== SERILOG =====
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.WithProperty("Service", "CRM")
    .WriteTo.Console());

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

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "CRM API", Version = "v1" });
    option.CustomSchemaIds(type => type.FullName);

    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter Token here:",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]))
    };
});

// DB Context
builder.Services.AddDbContext<CrmDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<ILoyaltyRuleRepository, LoyaltyRuleRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ILoyaltyTransactionRepository, LoyaltyTransactionRepository>();
builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();

// Services
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();

// AutoMapper
builder.Services.AddAutoMapper(cfg => {
    cfg.AddProfile<CrmProfile>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Protect internal APIs with shared key
app.UseInternalApiKeyProtection();

app.UseMiddleware<IdempotencyMiddleware>(); // Add Idempotency Middleware

// Serilog request logging - must be first to see final status codes
app.UseSerilogRequestLogging();

// Global Exception Handler - converts exceptions to proper HTTP responses
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
