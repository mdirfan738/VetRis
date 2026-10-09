using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Organizations;
using VetRIS.Domain.Repositories;
using VetRIS.Domain.ValueObjects;

namespace VetRIS.Application.Hospitals.Commands;

public record CreateHospitalCommand : IRequest<HospitalDto>
{
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressState { get; init; }
    public string? AddressPostalCode { get; init; }
    public string? AddressCountry { get; init; }
}

public class CreateHospitalCommandValidator : AbstractValidator<CreateHospitalCommand>
{
    public CreateHospitalCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CreateHospitalCommandHandler : IRequestHandler<CreateHospitalCommand, HospitalDto>
{
    private readonly IRepository<Hospital> _hospitals;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHospitalCommandHandler(IRepository<Hospital> hospitals, IUnitOfWork unitOfWork)
    {
        _hospitals = hospitals;
        _unitOfWork = unitOfWork;
    }

    public async Task<HospitalDto> Handle(CreateHospitalCommand request, CancellationToken cancellationToken)
    {
        Address? address = string.IsNullOrWhiteSpace(request.AddressLine1) || string.IsNullOrWhiteSpace(request.AddressCity)
            || string.IsNullOrWhiteSpace(request.AddressPostalCode) || string.IsNullOrWhiteSpace(request.AddressCountry)
            ? null
            : new Address(request.AddressLine1, request.AddressCity, request.AddressPostalCode, request.AddressCountry,
                request.AddressLine2, request.AddressState);

        var hospital = new Hospital(request.Name, request.Code, address, request.Phone, request.Email);

        await _hospitals.AddAsync(hospital, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return HospitalDto.FromEntity(hospital);
    }
}
