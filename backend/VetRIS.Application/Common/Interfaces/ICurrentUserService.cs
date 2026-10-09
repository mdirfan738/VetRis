namespace VetRIS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over "who is making this request". Consumed by
/// VetRISDbContext (to stamp CreatedBy/UpdatedBy/DeletedBy) and by
/// Application handlers that need to know the caller for authorization or
/// to pass along as a Domain method's changedBy parameter. Implemented by
/// the Web API layer against the authenticated ClaimsPrincipal/JWT once
/// Authentication &amp; Authorization is built.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>String form of the authenticated user's id — matches the type of BaseEntity.CreatedBy/UpdatedBy/DeletedBy.</summary>
    string? UserId { get; }
    string? UserName { get; }
}
