using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Billing.Commands;

public record CreateInvoiceCommand : IRequest<InvoiceDto>
{
    public string InvoiceNumber { get; init; } = default!;
    public Guid ClinicId { get; init; }
    public Guid? StudyId { get; init; }
    public DateTime DueAtUtc { get; init; }
    public string Currency { get; init; } = "INR";
}

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.DueAtUtc).GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.");
    }
}

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    private readonly IRepository<Invoice> _invoices;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInvoiceCommandHandler(IRepository<Invoice> invoices, IUnitOfWork unitOfWork)
    {
        _invoices = invoices;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceDto> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = new Invoice(request.InvoiceNumber, request.ClinicId, request.DueAtUtc, request.StudyId, request.Currency);

        await _invoices.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InvoiceDto.FromEntity(invoice);
    }
}
