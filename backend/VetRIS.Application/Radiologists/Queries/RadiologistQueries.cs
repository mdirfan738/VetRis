using MediatR;
using Microsoft.EntityFrameworkCore;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Radiologists.Queries;

public record GetRadiologistByIdQuery(Guid Id) : IRequest<RadiologistDto>;

public class GetRadiologistByIdQueryHandler : IRequestHandler<GetRadiologistByIdQuery, RadiologistDto>
{
    private readonly IRepository<Radiologist> _radiologists;

    public GetRadiologistByIdQueryHandler(IRepository<Radiologist> radiologists)
    {
        _radiologists = radiologists;
    }

    public async Task<RadiologistDto> Handle(GetRadiologistByIdQuery request, CancellationToken cancellationToken)
    {
        var radiologist = await _radiologists.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Radiologist), request.Id);

        return RadiologistDto.FromEntity(radiologist);
    }
}

public record GetRadiologistsQuery(bool ActiveOnly = true) : IRequest<IReadOnlyList<RadiologistDto>>;

public class GetRadiologistsQueryHandler : IRequestHandler<GetRadiologistsQuery, IReadOnlyList<RadiologistDto>>
{
    private readonly IRepository<Radiologist> _radiologists;

    public GetRadiologistsQueryHandler(IRepository<Radiologist> radiologists)
    {
        _radiologists = radiologists;
    }

    public async Task<IReadOnlyList<RadiologistDto>> Handle(GetRadiologistsQuery request, CancellationToken cancellationToken)
    {
        var radiologists = await _radiologists.GetQueryable()
            .Where(r => !request.ActiveOnly || r.IsActive)
            .OrderBy(r => r.LicenseNumber)
            .ToListAsync(cancellationToken);

        return radiologists.Select(RadiologistDto.FromEntity).ToList();
    }
}
