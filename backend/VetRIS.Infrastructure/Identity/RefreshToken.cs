namespace VetRIS.Infrastructure.Identity;

/// <summary>
/// Opaque, server-side-validated refresh token — deliberately NOT a JWT
/// (nothing to gain from making it self-describing, and a DB-backed opaque
/// token is trivially revocable, unlike a JWT you'd have to blocklist).
/// Lives beside Identity rather than in Domain — this is an authentication
/// artifact, not a business concept the domain experts would talk about.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    /// <summary>Set when rotated at refresh time, so a reused/stolen old token is detectable.</summary>
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
