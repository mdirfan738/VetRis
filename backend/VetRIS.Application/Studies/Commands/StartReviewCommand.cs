using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record StartReviewCommand : IRequest<StudyDto>
{
    public Guid StudyId { get; init; }
    public string? ChangedBy { get; init; }
}

public class StartReviewCommandValidator : AbstractValidator<StartReviewCommand>
{
    public StartReviewCommandValidator()
    {
        RuleFor(x => x.StudyId).NotEmpty();
    }
}

public class StartReviewCommandHandler : IRequestHandler<StartReviewCommand, StudyDto>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public StartReviewCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudyDto> Handle(StartReviewCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.StudyId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.StudyId);

        study.OpenForReview(request.ChangedBy);
        _studies.Update(study);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudyDto.FromEntity(study);
    }
}
