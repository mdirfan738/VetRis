using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record MarkDistributedCommand : IRequest<StudyDto>
{
    public Guid StudyId { get; init; }
    public string? ChangedBy { get; init; }
}

public class MarkDistributedCommandValidator : AbstractValidator<MarkDistributedCommand>
{
    public MarkDistributedCommandValidator() => RuleFor(x => x.StudyId).NotEmpty();
}

public class MarkDistributedCommandHandler : IRequestHandler<MarkDistributedCommand, StudyDto>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public MarkDistributedCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudyDto> Handle(MarkDistributedCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.StudyId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.StudyId);

        study.MarkDistributed(request.ChangedBy);
        _studies.Update(study);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudyDto.FromEntity(study);
    }
}
