using MediatR;
using Microsoft.EntityFrameworkCore;
using VetRIS.Domain.Entities.Reporting;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Reports.Queries;

public record GetReportByIdQuery(Guid Id) : IRequest<ReportDto>;

public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ReportDto>
{
    private readonly IRepository<Report> _reports;

    public GetReportByIdQueryHandler(IRepository<Report> reports)
    {
        _reports = reports;
    }

    public async Task<ReportDto> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Report), request.Id);

        return ReportDto.FromEntity(report);
    }
}

public record GetReportsByStudyQuery(Guid StudyId) : IRequest<IReadOnlyList<ReportDto>>;

public class GetReportsByStudyQueryHandler : IRequestHandler<GetReportsByStudyQuery, IReadOnlyList<ReportDto>>
{
    private readonly IRepository<Report> _reports;

    public GetReportsByStudyQueryHandler(IRepository<Report> reports)
    {
        _reports = reports;
    }

    public async Task<IReadOnlyList<ReportDto>> Handle(GetReportsByStudyQuery request, CancellationToken cancellationToken)
    {
        var reports = await _reports.GetQueryable()
            .Where(r => r.StudyId == request.StudyId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return reports.Select(ReportDto.FromEntity).ToList();
    }
}
