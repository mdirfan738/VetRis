namespace VetRIS.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 1,
    Sent = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Cancelled = 6,
    Refunded = 7,
    WrittenOff = 8
}
