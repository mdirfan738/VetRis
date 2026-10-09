using MediatR;
using VetRIS.Application.Common.Events;
using VetRIS.Domain.Entities.Ops;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Billing.EventHandlers;

public class PaymentReceivedEventHandler : INotificationHandler<DomainEventNotification<PaymentReceivedEvent>>
{
    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentReceivedEventHandler(IRepository<Notification> notifications, IUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<PaymentReceivedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var alert = new Notification(
            NotificationChannel.InApp,
            type: "PaymentReceived",
            subject: "Payment received",
            message: $"A payment of {domainEvent.Amount:N2} was received.",
            relatedEntityType: nameof(Domain.Entities.Billing.Invoice),
            relatedEntityId: domainEvent.InvoiceId);

        await _notifications.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // This is also the natural trigger point for the spec's Account
        // Scheduler / Day End Service reconciliation — left as a future
        // Infrastructure hosted-service phase rather than inline here.
    }
}
