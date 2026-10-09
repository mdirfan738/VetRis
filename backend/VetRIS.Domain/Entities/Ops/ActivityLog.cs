using VetRIS.Domain.Common;

namespace VetRIS.Domain.Entities.Ops;

/// <summary>Tracks user-facing activity (logins, worklist opens, exports) independent of entity-level auditing.</summary>
public class ActivityLog : BaseEntity
{
    public string UserId { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private ActivityLog()
    {
    } // EF Core

    public ActivityLog(string userId, string action, string? description = null, string? ipAddress = null, string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Action is required.", nameof(action));

        UserId = userId;
        Action = action;
        Description = description;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredAtUtc = DateTime.UtcNow;
    }
}
