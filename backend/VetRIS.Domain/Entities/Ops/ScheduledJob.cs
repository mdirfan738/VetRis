using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Ops;

/// <summary>Definition/config row for a recurring background job (Day End Service, Account Scheduler, etc.).</summary>
public class ScheduledJob : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string JobType { get; private set; } = default!; // e.g. "DayEndService", "StudyArchiveService"
    public JobFrequency Frequency { get; private set; }
    public string? CronExpression { get; private set; }
    public DateTime? LastRunAtUtc { get; private set; }
    public DateTime? NextRunAtUtc { get; private set; }
    public JobStatus LastStatus { get; private set; } = JobStatus.Scheduled;
    public bool IsEnabled { get; private set; } = true;

    private readonly List<JobExecutionLog> _executions = new();
    public IReadOnlyCollection<JobExecutionLog> Executions => _executions.AsReadOnly();

    private ScheduledJob()
    {
    } // EF Core

    public ScheduledJob(string name, string jobType, JobFrequency frequency, string? cronExpression = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Job name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(jobType)) throw new ArgumentException("Job type is required.", nameof(jobType));

        Name = name;
        JobType = jobType;
        Frequency = frequency;
        CronExpression = cronExpression;
    }

    public JobExecutionLog StartRun()
    {
        LastStatus = JobStatus.Running;
        LastRunAtUtc = DateTime.UtcNow;
        var execution = new JobExecutionLog(Id);
        _executions.Add(execution);
        return execution;
    }

    public void CompleteRun(JobExecutionLog execution, DateTime? nextRunAtUtc = null)
    {
        execution.Complete();
        LastStatus = JobStatus.Completed;
        NextRunAtUtc = nextRunAtUtc;
    }

    public void FailRun(JobExecutionLog execution, string errorMessage)
    {
        execution.Fail(errorMessage);
        LastStatus = JobStatus.Failed;
    }

    public void Disable() => IsEnabled = false;
    public void Enable() => IsEnabled = true;
}
