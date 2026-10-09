using VetRIS.Domain.Entities.Billing;

namespace VetRIS.Application.Billing;

public class InvoiceLineItemDto
{
    public Guid Id { get; init; }
    public string Description { get; init; } = default!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }

    public static InvoiceLineItemDto FromEntity(InvoiceLineItem item) => new()
    {
        Id = item.Id,
        Description = item.Description,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice.Amount,
        Amount = item.Amount.Amount
    };
}

public class InvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = default!;
    public Guid ClinicId { get; init; }
    public Guid? StudyId { get; init; }
    public decimal SubTotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal Outstanding { get; init; }
    public string Currency { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTime IssuedAtUtc { get; init; }
    public DateTime DueAtUtc { get; init; }
    public IReadOnlyList<InvoiceLineItemDto> LineItems { get; init; } = Array.Empty<InvoiceLineItemDto>();

    public static InvoiceDto FromEntity(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        ClinicId = invoice.ClinicId,
        StudyId = invoice.StudyId,
        SubTotal = invoice.SubTotal.Amount,
        TaxAmount = invoice.TaxAmount.Amount,
        TotalAmount = invoice.TotalAmount.Amount,
        AmountPaid = invoice.AmountPaid.Amount,
        Outstanding = invoice.Outstanding().Amount,
        Currency = invoice.TotalAmount.Currency,
        Status = invoice.Status.ToString(),
        IssuedAtUtc = invoice.IssuedAtUtc,
        DueAtUtc = invoice.DueAtUtc,
        LineItems = invoice.LineItems.Select(InvoiceLineItemDto.FromEntity).ToList()
    };
}
