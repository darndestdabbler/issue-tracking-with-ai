using IssueTracker.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IssueTracker.Tests;

public class IssueTrackerFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DatabaseProvider", "SQLite");

        builder.ConfigureServices(services =>
        {
            // Remove all AppDbContext registrations (options + provider services)
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType.FullName?.Contains("SqlServer") == true)
                .ToList();
            foreach (var d in descriptors)
                services.Remove(d);

            // Register AppDbContext with the shared in-memory SQLite connection
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        // Keep the connection open so the in-memory DB persists across requests
        await _connection.OpenAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
