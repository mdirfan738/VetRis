using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Application.Billing.Commands;

public record RegisterPaymentCommand : IRequest<PaymentDto>
{
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; }
    public string? ReceivedByUserId { get; init; }
    public string? TransactionReference { get; init; }
}

public class RegisterPaymentCommandValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).IsInEnum();
    }
}

public class RegisterPaymentCommandHandler : IRequestHandler<RegisterPaymentCommand, PaymentDto>
{
    private readonly IRepository<Invoice> _invoices;
    private readonly IRepository<Payment> _payments;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPaymentCommandHandler(IRepository<Invoice> invoices, IRepository<Payment> payments, IUnitOfWork unitOfWork)
    {
        _invoices = invoices;
        _payments = payments;
        _unitOfWork = unitOfWork;
    }

    // Touches two aggregates (Invoice + Payment) as one use case — "record a
    // payment" isn't meaningful split into two transactions, so this is a
    // deliberate exception to the single-aggregate-per-handler pattern used
    // everywhere else in this codebase.
    public async Task<PaymentDto> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Invoice), request.InvoiceId);

        var money = new Money(request.Amount, invoice.TotalAmount.Currency);
        var payment = new Payment(invoice.Id, money, request.Method, request.ReceivedByUserId, request.TransactionReference);
        payment.MarkCompleted();

        invoice.RegisterPayment(money);

        await _payments.AddAsync(payment, cancellationToken);
        _invoices.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PaymentDto.FromEntity(payment);
    }
}
