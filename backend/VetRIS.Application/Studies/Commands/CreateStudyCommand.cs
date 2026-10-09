using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Studies.Commands;

public record CreateStudyCommand : IRequest<StudyDto>
{
    public string AccessionNumber { get; init; } = default!;
    public Guid PatientId { get; init; }
    public Guid ClinicId { get; init; }
    public Guid? HospitalId { get; init; }
    public Modality Modality { get; init; }
    public StudyPriority Priority { get; init; } = StudyPriority.Routine;
    public string? ReferringPhysician { get; init; }
    public string? ClinicalHistory { get; init; }
    public string? BodyPart { get; init; }
}

public class CreateStudyCommandValidator : AbstractValidator<CreateStudyCommand>
{
    public CreateStudyCommandValidator()
    {
        RuleFor(x => x.AccessionNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.Modality).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class CreateStudyCommandHandler : IRequestHandler<CreateStudyCommand, StudyDto>
{
    private readonly IRepository<Study> _studies;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStudyCommandHandler(IRepository<Study> studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudyDto> Handle(CreateStudyCommand request, CancellationToken cancellationToken)
    {
        var study = new Study(
            request.AccessionNumber,
            request.PatientId,
            request.ClinicId,
            request.Modality,
            request.Priority,
            request.HospitalId,
            request.ReferringPhysician,
            request.ClinicalHistory,
            request.BodyPart);

        await _studies.AddAsync(study, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudyDto.FromEntity(study);
    }
}
