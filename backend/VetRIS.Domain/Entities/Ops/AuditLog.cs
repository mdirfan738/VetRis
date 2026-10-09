using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Ops;

/// <summary>Immutable record of a data change — who changed what, and the before/after values (serialized JSON).</summary>
public class AuditLog : BaseEntity
{
    public string EntityName { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public AuditAction Action { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string PerformedByUserId { get; private set; } = default!;
    public DateTime PerformedAtUtc { get; private set; }
    public string? IpAddress { get; private set; }

    private AuditLog()
    {
    } // EF Core

    public AuditLog(string entityName, string entityId, AuditAction action, string performedByUserId,
        string? oldValuesJson = null, string? newValuesJson = null, string? ipAddress = null)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        PerformedByUserId = performedByUserId;
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
        IpAddress = ipAddress;
        PerformedAtUtc = DateTime.UtcNow;
    }
}
