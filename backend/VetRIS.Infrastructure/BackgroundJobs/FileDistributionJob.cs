using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using VetRIS.Application.Studies.Commands;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;

namespace VetRIS.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec: "File Distribution Service — send reports to Clinics, Hospitals,
/// Doctors via Email/FTP/API." Actual transmission is a stub — no email/FTP
/// provider exists yet (that becomes an Application.Common.Interfaces
/// abstraction, implemented here, once one does). What this job does today
/// is real: find every study whose report has been written back to PACS
/// and drive its workflow status forward, so nothing downstream depends on
/// the stub to be workflow-correct.
/// </summary>
[DisallowConcurrentExecution]
public class FileDistributionJob : IJob
{
    public const string CronSchedule = "0 */15 * * * ?"; // every 15 minutes

    private readonly IRepository<Study> _studies;
    private readonly ISender _mediator;
    private readonly ILogger<FileDistributionJob> _logger;

    public FileDistributionJob(IRepository<Study> studies, ISender mediator, ILogger<FileDistributionJob> logger)
    {
        _studies = studies;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        var candidates = await _studies.GetQueryable()
            .Where(s => s.Status == StudyStatus.WrittenBack)
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var studyId in candidates)
        {
            try
            {
                // TODO(notifications/AWS integration phase): actually email
                // or FTP the signed report PDF to the clinic/referring
                // physician here before marking distributed.
                await _mediator.Send(new MarkDistributedCommand { StudyId = studyId, ChangedBy = "system:file-distribution-job" }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FileDistributionJob failed for study {StudyId}", studyId);
            }
        }

        if (candidates.Count > 0)
        {
            _logger.LogInformation("FileDistributionJob: distributed {Count} studies", candidates.Count);
        }
    }
}
