using MediatR;
using VetRIS.Domain.Common;

namespace VetRIS.Application.Common.Events;

/// <summary>
/// Wraps a Domain-layer IDomainEvent so it can be published/subscribed to
/// through MediatR without Domain taking a dependency on MediatR's
/// INotification. Infrastructure's VetRISDbContext constructs and publishes
/// these after SaveChangesAsync; Application handlers subscribe with
/// INotificationHandler&lt;DomainEventNotification&lt;StudyCreatedEvent&gt;&gt;, etc.
/// </summary>
public sealed class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
