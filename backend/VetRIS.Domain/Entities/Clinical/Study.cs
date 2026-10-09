using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Exceptions;

namespace VetRIS.Domain.Entities.Clinical;

/// <summary>
/// Aggregate root for the clinical workflow. Enforces the exact status
/// sequence from the spec: Created -> InProgress -> AssignedToRadiologist ->
/// UnderReview -> (ReviewRequired | ActionRequired) -> ReportGenerated ->
/// WrittenBack -> Distributed -> Finalized -> Archived, with Cancel
/// available from any non-terminal state. Each transition is recorded in
/// StatusHistory and raises a StudyStatusChangedEvent for the workflow
/// services (Study Archive, Day End, File Distribution, Study Writeback).
/// </summary>
public class Study : AggregateRoot
{
    public string AccessionNumber { get; private set; } = default!;
    public string? StudyInstanceUid { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid? HospitalId { get; private set; }
    public Modality Modality { get; private set; }
    public string? BodyPart { get; private set; }
    public StudyPriority Priority { get; private set; }
    public StudyStatus Status { get; private set; }
    public Guid? AssignedRadiologistId { get; private set; }
    public string? ReferringPhysician { get; private set; }
    public string? ClinicalHistory { get; private set; }
    public DateTime ScheduledAtUtc { get; private set; }
    public DateTime? StudyDateUtc { get; private set; }
    public string? Description { get; private set; }
    public bool IsCriticalFinding { get; private set; }

    private readonly List<Series> _series = new();
    public IReadOnlyCollection<Series> Series => _series.AsReadOnly();

    private readonly List<StudyStatusHistory> _statusHistory = new();
    public IReadOnlyCollection<StudyStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private readonly List<StudyComment> _comments = new();
    public IReadOnlyCollection<StudyComment> Comments => _comments.AsReadOnly();

    private Study()
    {
    } // EF Core

    public Study(string accessionNumber, Guid patientId, Guid clinicId, Modality modality, StudyPriority priority,
        Guid? hospitalId = null, string? referringPhysician = null, string? clinicalHistory = null, string? bodyPart = null)
    {
        if (string.IsNullOrWhiteSpace(accessionNumber))
            throw new DomainException("Accession number is required.");

        AccessionNumber = accessionNumber;
        PatientId = patientId;
        ClinicId = clinicId;
        HospitalId = hospitalId;
        Modality = modality;
        BodyPart = bodyPart;
        Priority = priority;
        ReferringPhysician = referringPhysician;
        ClinicalHistory = clinicalHistory;
        Status = StudyStatus.Created;
        ScheduledAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new StudyCreatedEvent(Id, AccessionNumber, PatientId, ClinicId));
    }

    public void AttachDicomStudyInstanceUid(string studyInstanceUid)
    {
        if (string.IsNullOrWhiteSpace(studyInstanceUid))
            throw new DomainException("StudyInstanceUID cannot be empty.");

        StudyInstanceUid = studyInstanceUid;
    }

    public void MarkInProgress(string? changedBy = null) => TransitionTo(StudyStatus.InProgress, changedBy);

    public void AssignRadiologist(Guid radiologistId, string? changedBy = null)
    {
        if (Status is StudyStatus.Archived or StudyStatus.Cancelled)
            throw new InvalidStudyStatusTransitionException(Status, StudyStatus.AssignedToRadiologist);

        AssignedRadiologistId = radiologistId;
        TransitionTo(StudyStatus.AssignedToRadiologist, changedBy);
    }

    public void OpenForReview(string? changedBy = null)
    {
        if (Status != StudyStatus.AssignedToRadiologist)
            throw new InvalidStudyStatusTransitionException(Status, StudyStatus.UnderReview);

        TransitionTo(StudyStatus.UnderReview, changedBy);
    }

    public void FlagReviewRequired(string? changedBy = null) => TransitionTo(StudyStatus.ReviewRequired, changedBy);

    public void RequireAction(string reason, string? changedBy = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A reason is required when a study is flagged for action.");

        AddComment(changedBy ?? "system", $"Action required: {reason}");
        TransitionTo(StudyStatus.ActionRequired, changedBy);
    }

    public void MarkReportGenerated(string? changedBy = null) => TransitionTo(StudyStatus.ReportGenerated, changedBy);

    public void MarkWrittenBack(string? changedBy = null) => TransitionTo(StudyStatus.WrittenBack, changedBy);

    public void MarkDistributed(string? changedBy = null) => TransitionTo(StudyStatus.Distributed, changedBy);

    public void MarkFinalized(string? changedBy = null) => TransitionTo(StudyStatus.Finalized, changedBy);

    public void Archive(string? changedBy = null)
    {
        if (Status != StudyStatus.Finalized)
            throw new InvalidStudyStatusTransitionException(Status, StudyStatus.Archived);

        TransitionTo(StudyStatus.Archived, changedBy);
    }

    public void Cancel(string reason, string? changedBy = null)
    {
        if (Status is StudyStatus.Archived)
            throw new InvalidStudyStatusTransitionException(Status, StudyStatus.Cancelled);

        AddComment(changedBy ?? "system", $"Cancelled: {reason}");
        TransitionTo(StudyStatus.Cancelled, changedBy);
    }

    public void MarkCritical() => IsCriticalFinding = true;

    public void AddSeries(Series series) => _series.Add(series);

    public void AddComment(string userId, string comment) => _comments.Add(new StudyComment(Id, userId, comment));

    private void TransitionTo(StudyStatus newStatus, string? changedBy)
    {
        var previous = Status;
        Status = newStatus;

        if (newStatus == StudyStatus.InProgress && StudyDateUtc is null)
            StudyDateUtc = DateTime.UtcNow;

        _statusHistory.Add(new StudyStatusHistory(Id, previous, newStatus, changedBy));
        RaiseDomainEvent(new StudyStatusChangedEvent(Id, previous, newStatus));
    }
}
