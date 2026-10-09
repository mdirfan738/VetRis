using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Clinical;

public class StudyStatusHistory : BaseEntity
{
    public Guid StudyId { get; private set; }
    public StudyStatus FromStatus { get; private set; }
    public StudyStatus ToStatus { get; private set; }
    public string? ChangedBy { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    private StudyStatusHistory()
    {
    } // EF Core

    public StudyStatusHistory(Guid studyId, StudyStatus fromStatus, StudyStatus toStatus, string? changedBy, string? notes = null)
    {
        StudyId = studyId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedBy = changedBy;
        ChangedAtUtc = DateTime.UtcNow;
        Notes = notes;
    }
}
