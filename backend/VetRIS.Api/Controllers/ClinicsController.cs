using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Clinics;
using VetRIS.Application.Clinics.Commands;
using VetRIS.Application.Clinics.Queries;

namespace VetRIS.Api.Controllers;

[Authorize]
public class ClinicsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "HospitalAdmin,SuperAdmin")]
    public async Task<ActionResult<ClinicDto>> Create(CreateClinicCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClinicDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetClinicByIdQuery(id), ct));

    [HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetClinicsQuery query, CancellationToken ct) =>
        Ok(await Mediator.Send(query, ct));
}
