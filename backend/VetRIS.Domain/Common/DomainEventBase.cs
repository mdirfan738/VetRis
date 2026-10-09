namespace VetRIS.Domain.Common;

/// <summary>
/// Common base for domain event records. Using records here (rather than
/// classes) gives each event value equality and an immutable payload for
/// free, which is exactly the shape a "fact that already happened" should have.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
