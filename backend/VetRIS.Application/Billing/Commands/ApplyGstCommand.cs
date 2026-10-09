using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Billing.Commands;

public record ApplyGstCommand : IRequest<InvoiceDto>
{
    public Guid InvoiceId { get; init; }
    /// <summary>Fraction, not percent — 0.18m for 18% GST.</summary>
    public decimal GstRate { get; init; }
}

public class ApplyGstCommandValidator : AbstractValidator<ApplyGstCommand>
{
    public ApplyGstCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.GstRate).InclusiveBetween(0m, 1m);
    }
}

public class ApplyGstCommandHandler : IRequestHandler<ApplyGstCommand, InvoiceDto>
{
    private readonly IRepository<Invoice> _invoices;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyGstCommandHandler(IRepository<Invoice> invoices, IUnitOfWork unitOfWork)
    {
        _invoices = invoices;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceDto> Handle(ApplyGstCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Invoice), request.InvoiceId);

        invoice.ApplyGst(request.GstRate);
        _invoices.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InvoiceDto.FromEntity(invoice);
    }
}
