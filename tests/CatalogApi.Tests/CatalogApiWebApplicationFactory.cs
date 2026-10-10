using CatalogApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogApi.Tests;

public class CatalogApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ProductsDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Create an in-memory SQLite connection that stays open for the test lifetime
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            // Add in-memory SQLite database for testing
            services.AddDbContext<ProductsDbContext>(options =>
                options.UseSqlite(_connection)
            );

            // Disable migration running for tests
            var configDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IConfiguration));
            if (configDescriptor is not null)
                services.Remove(configDescriptor);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Database:ApplyMigrationsOnStartup", "false" }
                })
                .Build();
            services.AddSingleton<IConfiguration>(config);

            // Replace real Entra ID auth with a fake scheme so tests don't need real tokens
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });

            // Create and seed the database
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
                dbContext.Database.EnsureCreated();
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection?.Dispose();
    }
}
