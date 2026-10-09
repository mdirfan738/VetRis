using MediatR;
using Microsoft.EntityFrameworkCore;
using VetRIS.Domain.Entities.Organizations;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Clinics.Queries;

public record GetClinicByIdQuery(Guid Id) : IRequest<ClinicDto>;

public class GetClinicByIdQueryHandler : IRequestHandler<GetClinicByIdQuery, ClinicDto>
{
    private readonly IRepository<Clinic> _clinics;

    public GetClinicByIdQueryHandler(IRepository<Clinic> clinics)
    {
        _clinics = clinics;
    }

    public async Task<ClinicDto> Handle(GetClinicByIdQuery request, CancellationToken cancellationToken)
    {
        var clinic = await _clinics.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Clinic), request.Id);

        return ClinicDto.FromEntity(clinic);
    }
}

public record GetClinicsQuery(Guid? HospitalId = null, bool ActiveOnly = true) : IRequest<IReadOnlyList<ClinicDto>>;

public class GetClinicsQueryHandler : IRequestHandler<GetClinicsQuery, IReadOnlyList<ClinicDto>>
{
    private readonly IRepository<Clinic> _clinics;

    public GetClinicsQueryHandler(IRepository<Clinic> clinics)
    {
        _clinics = clinics;
    }

    public async Task<IReadOnlyList<ClinicDto>> Handle(GetClinicsQuery request, CancellationToken cancellationToken)
    {
        var clinics = await _clinics.GetQueryable()
            .Where(c => !request.ActiveOnly || c.IsActive)
            .Where(c => request.HospitalId == null || c.HospitalId == request.HospitalId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return clinics.Select(ClinicDto.FromEntity).ToList();
    }
}
