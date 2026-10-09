using Microsoft.AspNetCore.Identity;

namespace VetRIS.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity role with Guid keys. Seeded once, at startup/migration
/// time, from every value of Domain's UserRoleType enum — see
/// IdentitySeeder (added alongside the Web API's startup wiring).
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
