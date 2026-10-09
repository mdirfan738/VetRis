using VetRIS.Domain.Common;

namespace VetRIS.Domain.Events;

public sealed record PaymentReceivedEvent(Guid PaymentId, Guid InvoiceId, decimal Amount) : DomainEventBase;
