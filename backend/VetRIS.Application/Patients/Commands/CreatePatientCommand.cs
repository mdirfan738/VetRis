using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Patients.Commands;

public record CreatePatientCommand : IRequest<PatientDto>
{
    public string Mrn { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public DateTime DateOfBirth { get; init; }
    public Gender Gender { get; init; } = Gender.Unspecified;
    public Guid ClinicId { get; init; }
    public Guid? HospitalId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
}

public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.Mrn).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, PatientDto>
{
    private readonly IRepository<Patient> _patients;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePatientCommandHandler(IRepository<Patient> patients, IUnitOfWork unitOfWork)
    {
        _patients = patients;
        _unitOfWork = unitOfWork;
    }

    public async Task<PatientDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = new Patient(
            request.Mrn, request.FirstName, request.LastName, request.DateOfBirth, request.Gender,
            request.ClinicId, request.HospitalId, request.Phone, request.Email);

        await _patients.AddAsync(patient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PatientDto.FromEntity(patient);
    }
}
