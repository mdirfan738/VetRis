using MediatR;
using VetRIS.Application.Common.Events;
using VetRIS.Domain.Entities.Ops;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.EventHandlers;

public class StudyStatusChangedEventHandler : INotificationHandler<DomainEventNotification<StudyStatusChangedEvent>>
{
    // Not every transition deserves a notification — only the ones a human
    // is actually waiting on.
    private static readonly StudyStatus[] NotifyOn =
    {
        StudyStatus.ActionRequired, StudyStatus.ReportGenerated, StudyStatus.Finalized
    };

    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public StudyStatusChangedEventHandler(IRepository<Notification> notifications, IUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<StudyStatusChangedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        if (!NotifyOn.Contains(domainEvent.ToStatus))
        {
            return;
        }

        var alert = new Notification(
            NotificationChannel.InApp,
            type: "StudyStatusChanged",
            subject: $"Study status: {domainEvent.ToStatus}",
            message: $"Study moved from {domainEvent.FromStatus} to {domainEvent.ToStatus}.",
            relatedEntityType: nameof(Domain.Entities.Clinical.Study),
            relatedEntityId: domainEvent.StudyId);

        await _notifications.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
