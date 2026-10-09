using MediatR;
using Microsoft.EntityFrameworkCore;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Queries;

public record GetRadiologistWorklistQuery(Guid RadiologistId) : IRequest<IReadOnlyList<StudyDto>>;

public class GetRadiologistWorklistQueryHandler : IRequestHandler<GetRadiologistWorklistQuery, IReadOnlyList<StudyDto>>
{
    private static readonly StudyStatus[] OpenStatuses =
    {
        StudyStatus.AssignedToRadiologist, StudyStatus.UnderReview, StudyStatus.ReviewRequired, StudyStatus.ActionRequired
    };

    private readonly IRepository<Study> _studies;

    public GetRadiologistWorklistQueryHandler(IRepository<Study> studies)
    {
        _studies = studies;
    }

    public async Task<IReadOnlyList<StudyDto>> Handle(GetRadiologistWorklistQuery request, CancellationToken cancellationToken)
    {
        var studies = await _studies.GetQueryable()
            .Where(s => s.AssignedRadiologistId == request.RadiologistId && OpenStatuses.Contains(s.Status))
            .OrderBy(s => s.Priority == StudyPriority.Emergency ? 0
                : s.Priority == StudyPriority.Stat ? 1
                : s.Priority == StudyPriority.Urgent ? 2
                : 3)
            .ThenBy(s => s.ScheduledAtUtc)
            .ToListAsync(cancellationToken);

        return studies.Select(StudyDto.FromEntity).ToList();
    }
}
