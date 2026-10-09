using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Events;
using VetRIS.Domain.Exceptions;

namespace VetRIS.Domain.Entities.Reporting;

/// <summary>
/// Aggregate root for the reporting workflow: Draft -> PendingApproval ->
/// (Approved | Rejected | ActionRequired) -> Signed -> Amended. Every edit
/// to a draft is captured as a ReportVersion before being overwritten, and
/// finalizing raises ReportFinalizedEvent for study writeback/distribution.
/// </summary>
public class Report : AggregateRoot
{
    public Guid StudyId { get; private set; }
    public Guid RadiologistId { get; private set; }
    public Guid? TemplateId { get; private set; }
    public string Findings { get; private set; } = default!;
    public string? Impression { get; private set; }
    public string? Recommendation { get; private set; }
    public ReportStatus Status { get; private set; }
    public int Version { get; private set; } = 1;
    public bool IsCritical { get; private set; }
    public string? SignatureData { get; private set; }
    public DateTime? SignedAtUtc { get; private set; }
    public DateTime? FinalizedAtUtc { get; private set; }
    public string? PdfStorageKey { get; private set; }

    private readonly List<ReportVersion> _versions = new();
    public IReadOnlyCollection<ReportVersion> Versions => _versions.AsReadOnly();

    private Report()
    {
    } // EF Core

    public Report(Guid studyId, Guid radiologistId, string findings, Guid? templateId = null)
    {
        if (string.IsNullOrWhiteSpace(findings)) throw new DomainException("Findings are required to start a report.");

        StudyId = studyId;
        RadiologistId = radiologistId;
        TemplateId = templateId;
        Findings = findings;
        Status = ReportStatus.Draft;
    }

    public void UpdateContent(string findings, string? impression, string? recommendation, string editedBy)
    {
        if (Status is ReportStatus.Finalized or ReportStatus.Signed)
            throw new DomainException("A finalized or signed report cannot be edited directly; amend it instead.");

        _versions.Add(new ReportVersion(Id, Version, Findings, Impression, editedBy));
        Findings = findings;
        Impression = impression;
        Recommendation = recommendation;
        Version++;
    }

    public void SubmitForApproval() => Status = ReportStatus.PendingApproval;

    public void Approve()
    {
        if (Status != ReportStatus.PendingApproval)
            throw new InvalidReportStatusTransitionException(Status, ReportStatus.Approved);

        Status = ReportStatus.Approved;
    }

    public void Reject(string reason)
    {
        if (Status != ReportStatus.PendingApproval)
            throw new InvalidReportStatusTransitionException(Status, ReportStatus.Rejected);
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A rejection reason is required.");

        Status = ReportStatus.Rejected;
    }

    public void RequireAction(string reason)
    {
        if (Status is ReportStatus.Finalized or ReportStatus.Signed)
            throw new InvalidReportStatusTransitionException(Status, ReportStatus.ActionRequired);
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A reason is required.");

        Status = ReportStatus.ActionRequired;
    }

    public void MarkCritical() => IsCritical = true;

    public void FinalizeAndSign(string signatureData, string? pdfStorageKey = null)
    {
        if (Status != ReportStatus.Approved)
            throw new InvalidReportStatusTransitionException(Status, ReportStatus.Signed);
        if (string.IsNullOrWhiteSpace(signatureData))
            throw new DomainException("A digital signature is required to finalize a report.");

        SignatureData = signatureData;
        SignedAtUtc = DateTime.UtcNow;
        FinalizedAtUtc = DateTime.UtcNow;
        PdfStorageKey = pdfStorageKey;
        Status = ReportStatus.Signed;

        RaiseDomainEvent(new ReportFinalizedEvent(Id, StudyId, RadiologistId));
    }

    public void Amend(string findings, string? impression, string editedBy)
    {
        if (Status != ReportStatus.Signed)
            throw new DomainException("Only a signed report can be amended.");

        _versions.Add(new ReportVersion(Id, Version, Findings, Impression, editedBy));
        Findings = findings;
        Impression = impression;
        Version++;
        Status = ReportStatus.Amended;
    }
}
