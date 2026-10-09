using MediatR;
using VetRIS.Application.Common.Events;
using VetRIS.Domain.Entities.Ops;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Reports.EventHandlers;

public class ReportFinalizedEventHandler : INotificationHandler<DomainEventNotification<ReportFinalizedEvent>>
{
    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public ReportFinalizedEventHandler(IRepository<Notification> notifications, IUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<ReportFinalizedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var alert = new Notification(
            NotificationChannel.InApp,
            type: "ReportFinalized",
            subject: "Report signed",
            message: "A report has been finalized and digitally signed.",
            relatedEntityType: nameof(Domain.Entities.Reporting.Report),
            relatedEntityId: domainEvent.ReportId);

        await _notifications.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The spec's Study Write Back / File Distribution services react to
        // this too (push the signed report back into PACS, fan it out to
        // the clinic) — those are Infrastructure-level integrations that
        // land in the PACS Integration / AWS S3 Integration phases.
    }
}
