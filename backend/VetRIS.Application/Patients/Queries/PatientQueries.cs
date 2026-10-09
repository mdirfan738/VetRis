using MediatR;
using Microsoft.EntityFrameworkCore;
using VetRIS.Application.Common.Models;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;

namespace VetRIS.Application.Patients.Queries;

public record GetPatientByIdQuery(Guid Id) : IRequest<PatientDto>;

public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, PatientDto>
{
    private readonly IRepository<Patient> _patients;

    public GetPatientByIdQueryHandler(IRepository<Patient> patients)
    {
        _patients = patients;
    }

    public async Task<PatientDto> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Patient), request.Id);

        return PatientDto.FromEntity(patient);
    }
}

public record SearchPatientsQuery : IRequest<PaginatedList<PatientDto>>
{
    public Guid? ClinicId { get; init; }
    /// <summary>Matches against MRN, first name, or last name (case-insensitive, partial).</summary>
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class SearchPatientsQueryHandler : IRequestHandler<SearchPatientsQuery, PaginatedList<PatientDto>>
{
    private readonly IRepository<Patient> _patients;

    public SearchPatientsQueryHandler(IRepository<Patient> patients)
    {
        _patients = patients;
    }

    public async Task<PaginatedList<PatientDto>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken)
    {
        var term = request.SearchTerm?.Trim().ToLowerInvariant();

        var query = _patients.GetQueryable()
            .Where(p => request.ClinicId == null || p.ClinicId == request.ClinicId)
            .Where(p => string.IsNullOrEmpty(term)
                || p.Mrn.ToLower().Contains(term)
                || p.FirstName.ToLower().Contains(term)
                || p.LastName.ToLower().Contains(term))
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName);

        var paged = await PaginatedList<Patient>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

        return new PaginatedList<PatientDto>(
            paged.Items.Select(PatientDto.FromEntity).ToList(),
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize);
    }
}
