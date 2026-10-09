using VetRIS.Domain.Common;

namespace VetRIS.Domain.Events;

public sealed record InvoiceGeneratedEvent(Guid InvoiceId, Guid ClinicId, decimal TotalAmount) : DomainEventBase;
