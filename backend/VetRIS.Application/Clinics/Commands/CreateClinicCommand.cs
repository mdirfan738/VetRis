using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Organizations;
using VetRIS.Domain.Repositories;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Application.Clinics.Commands;

public record CreateClinicCommand : IRequest<ClinicDto>
{
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public Guid? HospitalId { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressState { get; init; }
    public string? AddressPostalCode { get; init; }
    public string? AddressCountry { get; init; }
}

public class CreateClinicCommandValidator : AbstractValidator<CreateClinicCommand>
{
    public CreateClinicCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CreateClinicCommandHandler : IRequestHandler<CreateClinicCommand, ClinicDto>
{
    private readonly IRepository<Clinic> _clinics;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClinicCommandHandler(IRepository<Clinic> clinics, IUnitOfWork unitOfWork)
    {
        _clinics = clinics;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClinicDto> Handle(CreateClinicCommand request, CancellationToken cancellationToken)
    {
        Address? address = string.IsNullOrWhiteSpace(request.AddressLine1) || string.IsNullOrWhiteSpace(request.AddressCity)
            || string.IsNullOrWhiteSpace(request.AddressPostalCode) || string.IsNullOrWhiteSpace(request.AddressCountry)
            ? null
            : new Address(request.AddressLine1, request.AddressCity, request.AddressPostalCode, request.AddressCountry,
                request.AddressLine2, request.AddressState);

        var clinic = new Clinic(request.Name, request.Code, request.HospitalId, address, request.ContactPerson, request.Phone, request.Email);

        await _clinics.AddAsync(clinic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ClinicDto.FromEntity(clinic);
    }
}
