using CatalogApi.Data;
using CatalogApi.Data.Repositories;
using CatalogApi.Endpoints;
using CatalogApi.Models;
using CatalogApi.Middleware;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var databaseProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("CatalogDb");

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpLogging();

// EF Core & Database
if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ProductsDbContext>(options =>
        options.UseSqlServer(
            connectionString,
            sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
        )
    );
}
else // Default to SQLite
{
    builder.Services.AddDbContext<ProductsDbContext>(options =>
        options.UseSqlite(connectionString)
    );
}

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Application Insights (if connection string is configured)
var appInsightsConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductsDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy
    );

var app = builder.Build();

// Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpLogging();

// Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false // Liveness: no detailed checks, just responds 200
})
.WithName("Health")
.WithOpenApi();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Name == "database"
})
.WithName("HealthReady")
.WithOpenApi();

// Database migrations & seed
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
    var shouldApplyMigrations = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");

    if (shouldApplyMigrations)
    {
        await dbContext.Database.MigrateAsync();

        // Seed sample products in Development
        if (app.Environment.IsDevelopment() && !await dbContext.Products.AnyAsync())
        {
            dbContext.Products.AddRange(new[]
            {
                new Product
                {
                    Name = "Laptop",
                    Description = "High-performance laptop for developers",
                    Price = 1299.99m,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Monitor",
                    Description = "4K UltraHD monitor for professional work",
                    Price = 599.99m,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Keyboard",
                    Description = "Mechanical keyboard with RGB lighting",
                    Price = 199.99m,
                    CreatedAtUtc = DateTime.UtcNow
                }
            });

            await dbContext.SaveChangesAsync();
        }
    }
}

// Endpoints
app.MapProductEndpoints();

// Placeholder for authentication (to be added later with Entra ID)
// app.UseAuthentication();
// app.UseAuthorization();

var port = builder.Configuration.GetValue<int?>("PORT") ?? 8080;
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();

public partial class Program { }
