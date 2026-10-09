using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Application.Billing.Commands;

public record AddInvoiceLineItemCommand : IRequest<InvoiceDto>
{
    public Guid InvoiceId { get; init; }
    public string Description { get; init; } = default!;
    public int Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
}

public class AddInvoiceLineItemCommandValidator : AbstractValidator<AddInvoiceLineItemCommand>
{
    public AddInvoiceLineItemCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

public class AddInvoiceLineItemCommandHandler : IRequestHandler<AddInvoiceLineItemCommand, InvoiceDto>
{
    private readonly IRepository<Invoice> _invoices;
    private readonly IUnitOfWork _unitOfWork;

    public AddInvoiceLineItemCommandHandler(IRepository<Invoice> invoices, IUnitOfWork unitOfWork)
    {
        _invoices = invoices;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceDto> Handle(AddInvoiceLineItemCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Invoice), request.InvoiceId);

        invoice.AddLineItem(request.Description, request.Quantity, new Money(request.UnitPrice, invoice.SubTotal.Currency));
        _invoices.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InvoiceDto.FromEntity(invoice);
    }
}
