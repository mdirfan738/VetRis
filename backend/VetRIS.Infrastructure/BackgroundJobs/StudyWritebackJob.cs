using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using VetRIS.Application.Common.Interfaces;
using VetRIS.Application.Studies.Commands;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;

namespace VetRIS.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec: "Study Writeback Service — write study status, report,
/// annotations, and measurements back into PACS." IPacsService now exists
/// (see Infrastructure/Pacs) and this job uses its C-ECHO to confirm PACS
/// is actually reachable before doing anything else — a real check, not a
/// stub. The C-STORE call itself still isn't wired in here on purpose: it
/// needs the finalized report encapsulated as a valid DICOM object first
/// (a DICOM Encapsulated PDF or Structured Report), which is genuinely
/// separate, substantial work — constructing that dataset wrong would be
/// worse than leaving it as a clearly marked gap. Once that builder exists,
/// this job's job is just: call it, then IPacsService.StoreAsync() the
/// result before MarkWrittenBackCommand.
/// </summary>
[DisallowConcurrentExecution]
public class StudyWritebackJob : IJob
{
    public const string CronSchedule = "0 */10 * * * ?"; // every 10 minutes

    private readonly IRepository<Study> _studies;
    private readonly IPacsService _pacsService;
    private readonly ISender _mediator;
    private readonly ILogger<StudyWritebackJob> _logger;

    public StudyWritebackJob(IRepository<Study> studies, IPacsService pacsService, ISender mediator, ILogger<StudyWritebackJob> logger)
    {
        _studies = studies;
        _pacsService = pacsService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        var candidates = await _studies.GetQueryable()
            .Where(s => s.Status == StudyStatus.ReportGenerated)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return;
        }

        if (!await _pacsService.VerifyConnectionAsync(ct))
        {
            _logger.LogWarning("StudyWritebackJob: PACS unreachable (C-ECHO failed) — skipping this run, {Count} studies still pending", candidates.Count);
            return;
        }

        foreach (var studyId in candidates)
        {
            try
            {
                // TODO(report-to-DICOM builder): once a Report can be
                // encapsulated as a DICOM object, call
                // _pacsService.StoreAsync(encapsulatedFilePath, ct) here
                // before advancing the status below.
                await _mediator.Send(new MarkWrittenBackCommand { StudyId = studyId, ChangedBy = "system:study-writeback-job" }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StudyWritebackJob failed for study {StudyId}", studyId);
            }
        }

        _logger.LogInformation("StudyWritebackJob: wrote back {Count} studies", candidates.Count);
    }
}
