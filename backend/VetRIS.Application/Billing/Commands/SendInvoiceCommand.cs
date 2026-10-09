using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Billing.Commands;

public record SendInvoiceCommand(Guid InvoiceId) : IRequest<InvoiceDto>;

public class SendInvoiceCommandValidator : AbstractValidator<SendInvoiceCommand>
{
    public SendInvoiceCommandValidator() => RuleFor(x => x.InvoiceId).NotEmpty();
}

public class SendInvoiceCommandHandler : IRequestHandler<SendInvoiceCommand, InvoiceDto>
{
    private readonly IRepository<Invoice> _invoices;
    private readonly IUnitOfWork _unitOfWork;

    public SendInvoiceCommandHandler(IRepository<Invoice> invoices, IUnitOfWork unitOfWork)
    {
        _invoices = invoices;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceDto> Handle(SendInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Invoice), request.InvoiceId);

        invoice.Send();
        _invoices.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InvoiceDto.FromEntity(invoice);
    }
}
