using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record CancelStudyCommand : IRequest<StudyDto>
{
    public Guid StudyId { get; init; }
    public string Reason { get; init; } = default!;
    public string? ChangedBy { get; init; }
}

public class CancelStudyCommandValidator : AbstractValidator<CancelStudyCommand>
{
    public CancelStudyCommandValidator()
    {
        RuleFor(x => x.StudyId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class CancelStudyCommandHandler : IRequestHandler<CancelStudyCommand, StudyDto>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public CancelStudyCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudyDto> Handle(CancelStudyCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.StudyId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.StudyId);

        study.Cancel(request.Reason, request.ChangedBy);
        _studies.Update(study);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudyDto.FromEntity(study);
    }
}
