using CatalogApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogApi.Tests;

public class CatalogApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ProductsDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Add in-memory SQLite database for testing
            services.AddDbContext<ProductsDbContext>(options =>
                options.UseSqlite("Data Source=:memory:")
            );

            // Create and seed the database
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
                dbContext.Database.OpenConnection();
                dbContext.Database.EnsureCreated();
            }
        });
    }
}
