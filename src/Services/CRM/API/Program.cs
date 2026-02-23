using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using _360Retail.Services.CRM.Infrastructure.Persistence;
using _360Retail.Services.CRM.Infrastructure.Repositories;
using _360Retail.Services.CRM.Application.Services;
using _360Retail.Services.CRM.Application.Interfaces;
using _360Retail.Services.CRM.Application.Mappings;
using _360Retail.Services.CRM.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseMiddleware<IdempotencyMiddleware>(); // Add Idempotency Middleware

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
