using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Reports;
using VetRIS.Application.Reports.Commands;
using VetRIS.Application.Reports.Queries;

namespace VetRIS.Api.Controllers;

[Authorize(Roles = "Radiologist,HospitalAdmin,SuperAdmin")]
public class ReportsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<ReportDto>> Create(CreateReportCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetReportByIdQuery(id), ct));

    [HttpGet("by-study/{studyId:guid}")]
    public async Task<ActionResult> GetByStudy(Guid studyId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetReportsByStudyQuery(studyId), ct));

    public record UpdateContentBody(string Findings, string? Impression, string? Recommendation);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<ReportDto>> UpdateContent(Guid id, UpdateContentBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new UpdateReportContentCommand
        {
            ReportId = id,
            Findings = body.Findings,
            Impression = body.Impression,
            Recommendation = body.Recommendation,
            EditedBy = CurrentUser.UserId ?? "unknown"
        }, ct));

    [HttpPost("{id:guid}/submit-for-approval")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<ReportDto>> SubmitForApproval(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new SubmitReportForApprovalCommand(id), ct));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<ReportDto>> Approve(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new ApproveReportCommand(id), ct));

    public record ReasonBody(string Reason);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<ReportDto>> Reject(Guid id, ReasonBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new RejectReportCommand { ReportId = id, Reason = body.Reason }, ct));

    [HttpPost("{id:guid}/require-action")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<ReportDto>> RequireAction(Guid id, ReasonBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new RequireReportActionCommand { ReportId = id, Reason = body.Reason }, ct));

    public record SignBody(string SignatureData, string? PdfStorageKey);

    [HttpPost("{id:guid}/finalize-and-sign")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<ReportDto>> FinalizeAndSign(Guid id, SignBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new FinalizeAndSignReportCommand { ReportId = id, SignatureData = body.SignatureData, PdfStorageKey = body.PdfStorageKey }, ct));

    public record AmendBody(string Findings, string? Impression);

    [HttpPost("{id:guid}/amend")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<ReportDto>> Amend(Guid id, AmendBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new AmendReportCommand { ReportId = id, Findings = body.Findings, Impression = body.Impression, EditedBy = CurrentUser.UserId ?? "unknown" }, ct));
}
