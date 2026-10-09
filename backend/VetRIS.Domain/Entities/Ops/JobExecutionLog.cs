using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Ops;

public class JobExecutionLog : BaseEntity
{
    public Guid ScheduledJobId { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public JobStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    private JobExecutionLog()
    {
    } // EF Core

    public JobExecutionLog(Guid scheduledJobId)
    {
        ScheduledJobId = scheduledJobId;
        StartedAtUtc = DateTime.UtcNow;
        Status = JobStatus.Running;
    }

    public void Complete()
    {
        Status = JobStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = JobStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }
}
