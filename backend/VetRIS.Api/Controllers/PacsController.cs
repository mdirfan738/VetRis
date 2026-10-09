using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Common.Interfaces;

namespace VetRIS.Api.Controllers;

/// <summary>Diagnostic/query endpoints over IPacsService — no write path here yet (see StudyWritebackJob's caveat on why C-STORE isn't exposed until report-to-DICOM encapsulation exists).</summary>
[Authorize(Roles = "SuperAdmin,HospitalAdmin,Technician")]
public class PacsController : ApiControllerBase
{
    private readonly IPacsService _pacsService;

    public PacsController(IPacsService pacsService)
    {
        _pacsService = pacsService;
    }

    /// <summary>C-ECHO — confirms the configured PACS node is reachable.</summary>
    [HttpGet("verify")]
    public async Task<ActionResult> Verify(CancellationToken ct)
    {
        var reachable = await _pacsService.VerifyConnectionAsync(ct);
        return Ok(new { reachable });
    }

    /// <summary>C-FIND — study-level query against PACS (not this app's own database).</summary>
    [HttpGet("studies")]
    public async Task<ActionResult> FindStudies(
        [FromQuery] string? patientId, [FromQuery] string? accessionNumber, [FromQuery] string? studyDate, CancellationToken ct)
    {
        var results = await _pacsService.FindStudiesAsync(patientId, accessionNumber, studyDate, ct);
        return Ok(results);
    }

    /// <summary>C-MOVE — asks PACS to send a study to this application's configured destination AE.</summary>
    [HttpPost("studies/{studyInstanceUid}/retrieve")]
    public async Task<ActionResult> RetrieveStudy(string studyInstanceUid, CancellationToken ct)
    {
        var succeeded = await _pacsService.RetrieveStudyAsync(studyInstanceUid, ct);
        return succeeded ? Ok() : StatusCode(StatusCodes.Status502BadGateway, new { message = "C-MOVE did not succeed." });
    }
}
