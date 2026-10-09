using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record AddStudyCommentCommand : IRequest<Unit>
{
    public Guid StudyId { get; init; }
    public string UserId { get; init; } = default!;
    public string Comment { get; init; } = default!;
}

public class AddStudyCommentCommandValidator : AbstractValidator<AddStudyCommentCommand>
{
    public AddStudyCommentCommandValidator()
    {
        RuleFor(x => x.StudyId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
    }
}

public class AddStudyCommentCommandHandler : IRequestHandler<AddStudyCommentCommand, Unit>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public AddStudyCommentCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(AddStudyCommentCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.StudyId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.StudyId);

        study.AddComment(request.UserId, request.Comment);
        _studies.Update(study);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
