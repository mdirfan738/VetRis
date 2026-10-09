using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VetRIS.Infrastructure.Persistence;

namespace VetRIS.Api.IntegrationTests;

/// <summary>
/// Swaps VetRISDbContext's real MySQL provider for SQLite in-memory, and
/// supplies fake-but-non-empty config for the startup-time checks in
/// Program.cs / Infrastructure's DependencyInjection.cs (connection string,
/// JWT key) that would otherwise throw before ConfigureWebHost gets a
/// chance to override anything — those checks exist specifically to catch
/// a *missing* real value in production, and a test run needs to satisfy
/// them without providing one.
///
/// CAVEAT: this is the second-lowest-confidence piece of this solution
/// (after fo-dicom, for a different reason) — WebApplicationFactory +
/// IdentityDbContext + SQLite's EnsureCreated() path has real edge cases
/// I couldn't verify without a compiler. Run `dotnet test` on this project
/// specifically first if something here looks wrong; the Domain/Application
/// unit test projects need no hosting infrastructure at all and are much
/// higher-confidence.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:VetRISDb"] = "DataSource=placeholder-overridden-in-ConfigureServices",
                ["Jwt:Issuer"] = "VetRIS.Tests",
                ["Jwt:Audience"] = "VetRIS.Tests.Clients",
                ["Jwt:Key"] = "integration-test-only-signing-key-not-used-in-real-life-32chars",
                ["Jwt:ExpiryMinutes"] = "60",
                ["S3:BucketName"] = "test-bucket",
                ["Pacs:Host"] = "localhost"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<VetRISDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Keep one open connection for the whole factory lifetime — a
            // SQLite ":memory:" database only exists while a connection to
            // it is open, and each test class shares this factory instance.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<VetRISDbContext>(options => options.UseSqlite(_connection));

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<VetRISDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        _connection?.Dispose();
        base.Dispose(disposing);
    }
}
