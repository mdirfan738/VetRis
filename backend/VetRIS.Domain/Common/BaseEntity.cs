namespace VetRIS.Domain.Common;

/// <summary>
/// Base class for every domain entity. Carries identity plus the audit and
/// soft-delete columns required across the whole schema (CreatedAt/By,
/// UpdatedAt/By, IsDeleted, DeletedAt/By) so individual entities don't
/// have to repeat them. EF Core mapping (keys, indexes, global query
/// filters for IsDeleted) is configured entirely in the Infrastructure
/// layer — this class has no framework dependencies.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    protected BaseEntity() : this(Guid.NewGuid())
    {
    }

    protected BaseEntity(Guid id)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetCreatedBy(string? userId) => CreatedBy = userId;

    public void MarkUpdated(string? userId)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = userId;
    }

    public void SoftDelete(string? userId)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedBy = userId;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(BaseEntity? left, BaseEntity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !(left == right);
}
