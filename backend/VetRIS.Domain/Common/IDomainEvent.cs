namespace VetRIS.Domain.Common;

/// <summary>
/// Marker for something that happened in the domain that other parts of the
/// system (workflow services, notifications, PACS writeback) may care about.
/// Deliberately has no dependency on MediatR's INotification — the
/// Application layer will adapt these to MediatR notifications when it
/// dispatches them after SaveChanges succeeds, keeping Domain dependency-free.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}
