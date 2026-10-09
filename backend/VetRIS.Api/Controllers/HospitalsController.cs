using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Hospitals;
using VetRIS.Application.Hospitals.Commands;
using VetRIS.Application.Hospitals.Queries;

namespace VetRIS.Api.Controllers;

[Authorize]
public class HospitalsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<HospitalDto>> Create(CreateHospitalCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HospitalDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetHospitalByIdQuery(id), ct));

    [HttpGet]
    public async Task<ActionResult> GetList([FromQuery] bool activeOnly = true, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new GetHospitalsQuery(activeOnly), ct));
}
