using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Queries;

public record GetStudyByIdQuery(Guid Id) : IRequest<StudyDto>;

public class GetStudyByIdQueryHandler : IRequestHandler<GetStudyByIdQuery, StudyDto>
{
    private readonly IRepository<Study> _studies;

    public GetStudyByIdQueryHandler(IRepository<Study> studies)
    {
        _studies = studies;
    }

    public async Task<StudyDto> Handle(GetStudyByIdQuery request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.Id);

        return StudyDto.FromEntity(study);
    }
}
