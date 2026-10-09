using MediatR;
using VetRIS.Application.Common.Events;
using VetRIS.Domain.Entities.Ops;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Billing.EventHandlers;

public class InvoiceGeneratedEventHandler : INotificationHandler<DomainEventNotification<InvoiceGeneratedEvent>>
{
    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceGeneratedEventHandler(IRepository<Notification> notifications, IUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<InvoiceGeneratedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var alert = new Notification(
            NotificationChannel.Email,
            type: "InvoiceGenerated",
            subject: "New invoice",
            message: $"Invoice totalling {domainEvent.TotalAmount:N2} has been sent.",
            relatedEntityType: nameof(Domain.Entities.Billing.Invoice),
            relatedEntityId: domainEvent.InvoiceId);

        await _notifications.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
