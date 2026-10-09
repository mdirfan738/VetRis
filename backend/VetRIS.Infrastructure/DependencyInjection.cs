using Amazon.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VetRIS.Application.Common.Interfaces;
using VetRIS.Domain.Repositories;
using VetRIS.Infrastructure.Identity;
using VetRIS.Infrastructure.Pacs;
using VetRIS.Infrastructure.Persistence;
using VetRIS.Infrastructure.Repositories;
using VetRIS.Infrastructure.Storage;

namespace VetRIS.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Wires persistence, Identity, the repository/UoW pattern, and the PACS/S3 integrations. Called once from the Web API's Program.cs.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("VetRISDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:VetRISDb is not configured. Set it via user-secrets/environment/Secrets Manager for a real value.");
        }

        services.AddDbContext<VetRISDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;

                // Password policy — LAB MODE. Relaxed specifically so the
                // fixed "Admin" login required for this deployment lab
                // actually validates (it's 5 characters, no digit, no
                // symbol — the real-system defaults below would reject it
                // outright). Restore the stricter policy (commented) before
                // this ever holds real patient data or real user accounts.
                options.Password.RequiredLength = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                // Production policy — re-enable once lab credentials are gone:
                // options.Password.RequiredLength = 10;
                // options.Password.RequireDigit = true;
                // options.Password.RequireUppercase = true;
                // options.Password.RequireLowercase = true;
                // options.Password.RequireNonAlphanumeric = true;

                // Lockout policy
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<VetRISDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // --- PACS (ERAD, via fo-dicom) ---
        services.Configure<PacsOptions>(configuration.GetSection(PacsOptions.SectionName));
        services.AddScoped<IPacsService, FoDicomPacsService>();

        // --- AWS S3 ---
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.AddAWSService<IAmazonS3>();
        services.AddScoped<IFileStorageService, S3FileStorageService>();

        // ICurrentUserService (VetRIS.Application.Common.Interfaces) is
        // implemented in the Web API layer, reading the authenticated
        // ClaimsPrincipal, and registered there.

        return services;
    }
}
