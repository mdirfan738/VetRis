using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record AssignRadiologistCommand : IRequest<StudyDto>
{
    public Guid StudyId { get; init; }
    public Guid RadiologistId { get; init; }
    public string? ChangedBy { get; init; }
}

public class AssignRadiologistCommandValidator : AbstractValidator<AssignRadiologistCommand>
{
    public AssignRadiologistCommandValidator()
    {
        RuleFor(x => x.StudyId).NotEmpty();
        RuleFor(x => x.RadiologistId).NotEmpty();
    }
}

public class AssignRadiologistCommandHandler : IRequestHandler<AssignRadiologistCommand, StudyDto>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRadiologistCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudyDto> Handle(AssignRadiologistCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.StudyId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.StudyId);

        study.AssignRadiologist(request.RadiologistId, request.ChangedBy);
        _studies.Update(study);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudyDto.FromEntity(study);
    }
}
