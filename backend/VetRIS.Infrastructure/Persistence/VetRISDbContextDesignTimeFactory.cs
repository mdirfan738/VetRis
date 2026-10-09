using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VetRIS.Application.Common.Interfaces;

namespace VetRIS.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` / `dotnet ef database update` construct a
/// VetRISDbContext without the ASP.NET Core host — EF's tooling looks for
/// this interface automatically. Reads the connection string from an
/// environment variable so no secrets are hard-coded here; the real app
/// wires ICurrentUserService/IPublisher for real at runtime instead of
/// these no-op stand-ins.
/// </summary>
public class VetRISDbContextDesignTimeFactory : IDesignTimeDbContextFactory<VetRISDbContext>
{
    public VetRISDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("VETRIS_DB_CONNECTION")
            ?? "server=localhost;port=3306;database=vetris;user=root;password=changeme";

        var optionsBuilder = new DbContextOptionsBuilder<VetRISDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new VetRISDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService(), new DesignTimePublisher());
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
    }

    private sealed class DesignTimePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
