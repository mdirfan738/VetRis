namespace VetRIS.Domain.Common;

/// <summary>
/// Marks an entity as an aggregate root and transaction boundary, and gives
/// it a place to collect domain events raised during its lifetime. The
/// Infrastructure-layer DbContext (added in the next phase) will walk
/// tracked aggregates after a successful SaveChanges, dispatch their
/// events through MediatR, then clear them.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
