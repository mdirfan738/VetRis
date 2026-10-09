using FluentValidation;
using MediatR;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Radiologists.Commands;

/// <summary>
/// Assumes the Identity account (ApplicationUser, role = Radiologist)
/// already exists — created during Authentication &amp; Authorization's
/// registration flow. This command only creates the RIS-specific profile
/// Domain actually reasons about (license, specialization, workload cap).
/// </summary>
public record CreateRadiologistCommand : IRequest<RadiologistDto>
{
    public Guid ApplicationUserId { get; init; }
    public string LicenseNumber { get; init; } = default!;
    public string? Specialization { get; init; }
    public int MaxDailyWorkload { get; init; } = 20;
}

public class CreateRadiologistCommandValidator : AbstractValidator<CreateRadiologistCommand>
{
    public CreateRadiologistCommandValidator()
    {
        RuleFor(x => x.ApplicationUserId).NotEmpty();
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxDailyWorkload).GreaterThan(0);
    }
}

public class CreateRadiologistCommandHandler : IRequestHandler<CreateRadiologistCommand, RadiologistDto>
{
    private readonly IRepository<Radiologist> _radiologists;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRadiologistCommandHandler(IRepository<Radiologist> radiologists, IUnitOfWork unitOfWork)
    {
        _radiologists = radiologists;
        _unitOfWork = unitOfWork;
    }

    public async Task<RadiologistDto> Handle(CreateRadiologistCommand request, CancellationToken cancellationToken)
    {
        var radiologist = new Radiologist(request.ApplicationUserId, request.LicenseNumber, request.Specialization, request.MaxDailyWorkload);

        await _radiologists.AddAsync(radiologist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RadiologistDto.FromEntity(radiologist);
    }
}
