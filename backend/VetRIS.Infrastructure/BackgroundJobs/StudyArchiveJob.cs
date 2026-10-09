using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using VetRIS.Application.Common.Interfaces;
using VetRIS.Application.Studies.Commands;
using VetRIS.Domain.Entities.Clinical;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Repositories;
using VetRIS.Infrastructure.Storage;

namespace VetRIS.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec: "Study Archive Service — move completed studies to AWS S3." Finds
/// every Finalized study, moves each of its DicomImage files to the
/// configured cold-storage prefix, then drives the study through
/// Study.Archive() via the existing ArchiveStudyCommand — the same command
/// path an admin's manual override in the UI would use.
/// </summary>
[DisallowConcurrentExecution]
public class StudyArchiveJob : IJob
{
    public const string CronSchedule = "0 0 2 * * ?"; // 02:00 daily

    private readonly IRepository<Study> _studies;
    private readonly IFileStorageService _fileStorage;
    private readonly ISender _mediator;
    private readonly S3Options _s3Options;
    private readonly ILogger<StudyArchiveJob> _logger;

    public StudyArchiveJob(
        IRepository<Study> studies,
        IFileStorageService fileStorage,
        ISender mediator,
        IOptions<S3Options> s3Options,
        ILogger<StudyArchiveJob> logger)
    {
        _studies = studies;
        _fileStorage = fileStorage;
        _mediator = mediator;
        _s3Options = s3Options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        var candidateIds = await _studies.GetQueryable()
            .Where(s => s.Status == StudyStatus.Finalized)
            .Select(s => s.Id)
            .ToListAsync(ct);

        _logger.LogInformation("StudyArchiveJob: {Count} finalized studies ready to archive", candidateIds.Count);

        foreach (var studyId in candidateIds)
        {
            try
            {
                // Series/Images are AutoIncluded (see SeriesConfiguration /
                // StudyConfiguration), so this one load has everything
                // needed to move every file for the study.
                var study = await _studies.GetByIdAsync(studyId, ct);
                if (study is null)
                {
                    continue;
                }

                foreach (var series in study.Series)
                {
                    foreach (var image in series.Images)
                    {
                        var archiveKey = $"{_s3Options.ArchivePrefix.TrimEnd('/')}/{image.StorageKey.TrimStart('/')}";
                        await _fileStorage.MoveAsync(image.StorageKey, archiveKey, ct);
                    }
                }

                await _mediator.Send(new ArchiveStudyCommand { StudyId = studyId, ChangedBy = "system:study-archive-job" }, ct);
            }
            catch (Exception ex)
            {
                // One study's S3 failure shouldn't block the rest of the
                // batch — it's simply retried on tomorrow's run since its
                // status won't have changed.
                _logger.LogError(ex, "StudyArchiveJob failed for study {StudyId}", studyId);
            }
        }
    }
}
