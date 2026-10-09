using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record MarkFinalizedCommand : IRequest<StudyDto>
{
    public Guid StudyId { get; init; }
    public string? ChangedBy { get; init; }
}

public class MarkFinalizedCommandValidator : AbstractValidator<MarkFinalizedCommand>
{
    public MarkFinalizedCommandValidator() => RuleFor(x => x.StudyId).NotEmpty();
}

public class MarkFinalizedCommandHandler : IRequestHandler<MarkFinalizedCommand, StudyDto>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public MarkFinalizedCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudyDto> Handle(MarkFinalizedCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.StudyId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.StudyId);

        study.MarkFinalized(request.ChangedBy);
        _studies.Update(study);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudyDto.FromEntity(study);
    }
}
