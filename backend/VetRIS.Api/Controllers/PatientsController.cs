using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Patients;
using VetRIS.Application.Patients.Commands;
using VetRIS.Application.Patients.Queries;

namespace VetRIS.Api.Controllers;

[Authorize(Roles = "Receptionist,ClinicUser,Technician,HospitalAdmin,SuperAdmin")]
public class PatientsController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPatientByIdQuery(id), ct));

    [HttpGet]
    public async Task<ActionResult> Search([FromQuery] SearchPatientsQuery query, CancellationToken ct) =>
        Ok(await Mediator.Send(query, ct));
}
