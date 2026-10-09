using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Domain.Entities.Billing;

public class Payment : AggregateRoot
{
    public Guid InvoiceId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? TransactionReference { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public string? ReceivedByUserId { get; private set; }

    private Payment()
    {
    } // EF Core

    public Payment(Guid invoiceId, Money amount, PaymentMethod method, string? receivedByUserId = null, string? transactionReference = null)
    {
        if (amount.Amount <= 0) throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));

        InvoiceId = invoiceId;
        Amount = amount;
        Method = method;
        ReceivedByUserId = receivedByUserId;
        TransactionReference = transactionReference;
        Status = PaymentStatus.Pending;
    }

    public void MarkCompleted()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Only a pending payment can be completed (current status: '{Status}').");

        Status = PaymentStatus.Completed;
        PaidAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new PaymentReceivedEvent(Id, InvoiceId, Amount.Amount));
    }

    public void MarkFailed() => Status = PaymentStatus.Failed;

    public void Refund(bool partial = false)
    {
        if (Status != PaymentStatus.Completed)
            throw new DomainException("Only a completed payment can be refunded.");

        Status = partial ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;
    }

    public void Cancel()
    {
        if (Status == PaymentStatus.Completed)
            throw new DomainException("A completed payment cannot be cancelled; issue a refund instead.");

        Status = PaymentStatus.Cancelled;
    }
}
