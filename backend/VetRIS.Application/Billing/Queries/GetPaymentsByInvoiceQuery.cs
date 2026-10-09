using MediatR;
using Microsoft.EntityFrameworkCore;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Billing.Queries;

public record GetPaymentsByInvoiceQuery(Guid InvoiceId) : IRequest<IReadOnlyList<PaymentDto>>;

public class GetPaymentsByInvoiceQueryHandler : IRequestHandler<GetPaymentsByInvoiceQuery, IReadOnlyList<PaymentDto>>
{
    private readonly IRepository<Payment> _payments;

    public GetPaymentsByInvoiceQueryHandler(IRepository<Payment> payments)
    {
        _payments = payments;
    }

    public async Task<IReadOnlyList<PaymentDto>> Handle(GetPaymentsByInvoiceQuery request, CancellationToken cancellationToken)
    {
        var payments = await _payments.GetQueryable()
            .Where(p => p.InvoiceId == request.InvoiceId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return payments.Select(PaymentDto.FromEntity).ToList();
    }
}
