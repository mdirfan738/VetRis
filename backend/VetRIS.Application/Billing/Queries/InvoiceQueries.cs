using MediatR;
using VetRIS.Application.Common.Models;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Billing.Queries;

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDto>;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly IRepository<Invoice> _invoices;

    public GetInvoiceByIdQueryHandler(IRepository<Invoice> invoices)
    {
        _invoices = invoices;
    }

    public async Task<InvoiceDto> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Invoice), request.Id);

        return InvoiceDto.FromEntity(invoice);
    }
}

public record GetInvoicesQuery : IRequest<PaginatedList<InvoiceDto>>
{
    public Guid? ClinicId { get; init; }
    public InvoiceStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, PaginatedList<InvoiceDto>>
{
    private readonly IRepository<Invoice> _invoices;

    public GetInvoicesQueryHandler(IRepository<Invoice> invoices)
    {
        _invoices = invoices;
    }

    public async Task<PaginatedList<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = _invoices.GetQueryable()
            .Where(i => request.ClinicId == null || i.ClinicId == request.ClinicId)
            .Where(i => request.Status == null || i.Status == request.Status)
            .OrderByDescending(i => i.IssuedAtUtc);

        var paged = await PaginatedList<Invoice>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

        return new PaginatedList<InvoiceDto>(
            paged.Items.Select(InvoiceDto.FromEntity).ToList(),
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize);
    }
}
