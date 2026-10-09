using MediatR;
using Microsoft.EntityFrameworkCore;
using VetRIS.Application.Common.Models;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Queries;

public record GetStudiesQuery : IRequest<PaginatedList<StudyDto>>
{
    public StudyStatus? Status { get; init; }
    public Guid? ClinicId { get; init; }
    public Guid? AssignedRadiologistId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetStudiesQueryHandler : IRequestHandler<GetStudiesQuery, PaginatedList<StudyDto>>
{
    private readonly IRepository<Study> _studies;

    public GetStudiesQueryHandler(IRepository<Study> studies)
    {
        _studies = studies;
    }

    public async Task<PaginatedList<StudyDto>> Handle(GetStudiesQuery request, CancellationToken cancellationToken)
    {
        var query = _studies.GetQueryable()
            .Where(s => request.Status == null || s.Status == request.Status)
            .Where(s => request.ClinicId == null || s.ClinicId == request.ClinicId)
            .Where(s => request.AssignedRadiologistId == null || s.AssignedRadiologistId == request.AssignedRadiologistId)
            .OrderByDescending(s => s.ScheduledAtUtc);

        var paged = await PaginatedList<Study>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

        return new PaginatedList<StudyDto>(
            paged.Items.Select(StudyDto.FromEntity).ToList(),
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize);
    }
}
