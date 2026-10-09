using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record ArchiveStudyCommand : IRequest<StudyDto>
{
    public Guid StudyId { get; init; }
    public string? ChangedBy { get; init; }
}

public class ArchiveStudyCommandValidator : AbstractValidator<ArchiveStudyCommand>
{
    public ArchiveStudyCommandValidator() => RuleFor(x => x.StudyId).NotEmpty();
}

public class ArchiveStudyCommandHandler : IRequestHandler<ArchiveStudyCommand, StudyDto>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveStudyCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudyDto> Handle(ArchiveStudyCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.GetByIdAsync(request.StudyId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Study), request.StudyId);

        // Note: this is a direct archive call. The spec's Study Archive
        // Service (a later phase) also moves DICOM files to S3 first —
        // this command only flips the workflow status; the background
        // service and this command both end up calling study.Archive().
        study.Archive(request.ChangedBy);
        _studies.Update(study);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudyDto.FromEntity(study);
    }
}
