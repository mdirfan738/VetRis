using VetRIS.Domain.Entities.Billing;

namespace VetRIS.Application.Billing;

public class PaymentDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string Method { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? TransactionReference { get; init; }
    public DateTime? PaidAtUtc { get; init; }

    public static PaymentDto FromEntity(Payment payment) => new()
    {
        Id = payment.Id,
        InvoiceId = payment.InvoiceId,
        Amount = payment.Amount.Amount,
        Currency = payment.Amount.Currency,
        Method = payment.Method.ToString(),
        Status = payment.Status.ToString(),
        TransactionReference = payment.TransactionReference,
        PaidAtUtc = payment.PaidAtUtc
    };
}
