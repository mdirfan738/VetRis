using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Studies;
using VetRIS.Application.Studies.Commands;
using VetRIS.Application.Studies.Queries;

namespace VetRIS.Api.Controllers;

[Authorize]
public class StudiesController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "ClinicUser,Technician,HospitalAdmin,SuperAdmin")]
    public async Task<ActionResult<StudyDto>> Create(CreateStudyCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudyDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetStudyByIdQuery(id), ct));

    [HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetStudiesQuery query, CancellationToken ct) =>
        Ok(await Mediator.Send(query, ct));

    [HttpGet("worklist/{radiologistId:guid}")]
    [Authorize(Roles = "Radiologist,HospitalAdmin,SuperAdmin")]
    public async Task<ActionResult> GetWorklist(Guid radiologistId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetRadiologistWorklistQuery(radiologistId), ct));

    public record AssignRadiologistBody(Guid RadiologistId);

    [HttpPost("{id:guid}/assign-radiologist")]
    [Authorize(Roles = "HospitalAdmin,SuperAdmin,Technician")]
    public async Task<ActionResult<StudyDto>> AssignRadiologist(Guid id, AssignRadiologistBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new AssignRadiologistCommand { StudyId = id, RadiologistId = body.RadiologistId, ChangedBy = CurrentUser.UserId }, ct));

    [HttpPost("{id:guid}/start-review")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<StudyDto>> StartReview(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new StartReviewCommand { StudyId = id, ChangedBy = CurrentUser.UserId }, ct));

    [HttpPost("{id:guid}/flag-review-required")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<StudyDto>> FlagReviewRequired(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new FlagReviewRequiredCommand { StudyId = id, ChangedBy = CurrentUser.UserId }, ct));

    public record ReasonBody(string Reason);

    [HttpPost("{id:guid}/require-action")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<StudyDto>> RequireAction(Guid id, ReasonBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new RequireActionCommand { StudyId = id, Reason = body.Reason, ChangedBy = CurrentUser.UserId }, ct));

    [HttpPost("{id:guid}/mark-report-generated")]
    [Authorize(Roles = "Radiologist,SuperAdmin")]
    public async Task<ActionResult<StudyDto>> MarkReportGenerated(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new MarkReportGeneratedCommand { StudyId = id, ChangedBy = CurrentUser.UserId }, ct));

    [HttpPost("{id:guid}/mark-written-back")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
    public async Task<ActionResult<StudyDto>> MarkWrittenBack(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new MarkWrittenBackCommand { StudyId = id, ChangedBy = CurrentUser.UserId }, ct));

    [HttpPost("{id:guid}/mark-distributed")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
    public async Task<ActionResult<StudyDto>> MarkDistributed(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new MarkDistributedCommand { StudyId = id, ChangedBy = CurrentUser.UserId }, ct));

    [HttpPost("{id:guid}/finalize")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
    public async Task<ActionResult<StudyDto>> FinalizeStudy(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new MarkFinalizedCommand { StudyId = id, ChangedBy = CurrentUser.UserId }, ct));

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
    public async Task<ActionResult<StudyDto>> Archive(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new ArchiveStudyCommand { StudyId = id, ChangedBy = CurrentUser.UserId }, ct));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
    public async Task<ActionResult<StudyDto>> Cancel(Guid id, ReasonBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new CancelStudyCommand { StudyId = id, Reason = body.Reason, ChangedBy = CurrentUser.UserId }, ct));

    public record CommentBody(string Comment);

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult> AddComment(Guid id, CommentBody body, CancellationToken ct)
    {
        await Mediator.Send(new AddStudyCommentCommand { StudyId = id, UserId = CurrentUser.UserId ?? "unknown", Comment = body.Comment }, ct);
        return NoContent();
    }
}
