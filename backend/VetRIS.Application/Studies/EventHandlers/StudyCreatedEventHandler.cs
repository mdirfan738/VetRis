using MediatR;
using VetRIS.Application.Common.Events;
using VetRIS.Domain.Entities.Ops;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.EventHandlers;

/// <summary>
/// Reacts to a new study by queuing an in-app notification. Stands in for
/// the spec's "workflow services" reacting to intake — actual email/SMS
/// delivery is added once the Infrastructure providers exist (SES/SNS or
/// similar); this proves the dispatch pipeline wired up in VetRISDbContext
/// actually reaches a handler.
/// </summary>
public class StudyCreatedEventHandler : INotificationHandler<DomainEventNotification<StudyCreatedEvent>>
{
    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public StudyCreatedEventHandler(IRepository<Notification> notifications, IUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<StudyCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var alert = new Notification(
            NotificationChannel.InApp,
            type: "StudyCreated",
            subject: "New study received",
            message: $"Study {domainEvent.AccessionNumber} was created and is awaiting processing.",
            relatedEntityType: nameof(Domain.Entities.Clinical.Study),
            relatedEntityId: domainEvent.StudyId);

        await _notifications.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
