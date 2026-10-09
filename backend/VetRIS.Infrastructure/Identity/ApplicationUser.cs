using Microsoft.AspNetCore.Identity;

namespace VetRIS.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity user with Guid keys. Login, password hash, MFA secret,
/// and lockout state are all handled by the base IdentityUser — this only
/// adds the display/audit fields the RIS UI needs. Business-specific profile
/// data for the Radiologist role lives in Domain's Radiologist entity,
/// linked by Id, not duplicated here.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public Guid? ClinicId { get; set; }
    public Guid? HospitalId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
