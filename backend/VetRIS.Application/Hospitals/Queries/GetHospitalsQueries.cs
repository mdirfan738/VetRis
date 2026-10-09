using MediatR;
using Microsoft.EntityFrameworkCore;
using VetRIS.Domain.Entities.Organizations;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Hospitals.Queries;

public record GetHospitalByIdQuery(Guid Id) : IRequest<HospitalDto>;

public class GetHospitalByIdQueryHandler : IRequestHandler<GetHospitalByIdQuery, HospitalDto>
{
    private readonly IRepository<Hospital> _hospitals;

    public GetHospitalByIdQueryHandler(IRepository<Hospital> hospitals)
    {
        _hospitals = hospitals;
    }

    public async Task<HospitalDto> Handle(GetHospitalByIdQuery request, CancellationToken cancellationToken)
    {
        var hospital = await _hospitals.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Hospital), request.Id);

        return HospitalDto.FromEntity(hospital);
    }
}

public record GetHospitalsQuery(bool ActiveOnly = true) : IRequest<IReadOnlyList<HospitalDto>>;

public class GetHospitalsQueryHandler : IRequestHandler<GetHospitalsQuery, IReadOnlyList<HospitalDto>>
{
    private readonly IRepository<Hospital> _hospitals;

    public GetHospitalsQueryHandler(IRepository<Hospital> hospitals)
    {
        _hospitals = hospitals;
    }

    public async Task<IReadOnlyList<HospitalDto>> Handle(GetHospitalsQuery request, CancellationToken cancellationToken)
    {
        var hospitals = await _hospitals.GetQueryable()
            .Where(h => !request.ActiveOnly || h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);

        return hospitals.Select(HospitalDto.FromEntity).ToList();
    }
}
