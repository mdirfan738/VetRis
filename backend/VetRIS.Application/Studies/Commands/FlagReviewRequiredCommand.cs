using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record FlagReviewRequiredCommand : IRequest<StudyDto>
{
    public Guid StudyId { get; init; }
    public string? ChangedBy { get; init; }
}

public class FlagReviewRequiredCommandValidator : AbstractValidator<FlagReviewRequiredCommand>
{
    public FlagReviewRequiredCommandValidator() => RuleFor(x => x.StudyId).NotEmpty();
}

public class FlagReviewRequiredCommandHandler : IRequestHandler<FlagReviewRequiredCommand, StudyDto>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public FlagReviewRequiredCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudyDto> Handle(FlagReviewRequiredCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.StudyId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.StudyId);

        study.FlagReviewRequired(request.ChangedBy);
        _studies.Update(study);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudyDto.FromEntity(study);
    }
}
