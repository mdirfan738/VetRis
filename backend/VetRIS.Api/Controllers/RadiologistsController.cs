using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Radiologists;
using VetRIS.Application.Radiologists.Commands;
using VetRIS.Application.Radiologists.Queries;

namespace VetRIS.Api.Controllers;

[Authorize]
public class RadiologistsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "HospitalAdmin,SuperAdmin")]
    public async Task<ActionResult<RadiologistDto>> Create(CreateRadiologistCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RadiologistDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetRadiologistByIdQuery(id), ct));

    [HttpGet]
    public async Task<ActionResult> GetList([FromQuery] bool activeOnly = true, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new GetRadiologistsQuery(activeOnly), ct));
}
