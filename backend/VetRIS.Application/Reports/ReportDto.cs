using VetRIS.Domain.Entities.Reporting;

namespace VetRIS.Application.Reports;

public class ReportDto
{
    public Guid Id { get; init; }
    public Guid StudyId { get; init; }
    public Guid RadiologistId { get; init; }
    public Guid? TemplateId { get; init; }
    public string Findings { get; init; } = default!;
    public string? Impression { get; init; }
    public string? Recommendation { get; init; }
    public string Status { get; init; } = default!;
    public int Version { get; init; }
    public bool IsCritical { get; init; }
    public DateTime? SignedAtUtc { get; init; }
    public DateTime? FinalizedAtUtc { get; init; }
    public string? PdfStorageKey { get; init; }

    public static ReportDto FromEntity(Report report) => new()
    {
        Id = report.Id,
        StudyId = report.StudyId,
        RadiologistId = report.RadiologistId,
        TemplateId = report.TemplateId,
        Findings = report.Findings,
        Impression = report.Impression,
        Recommendation = report.Recommendation,
        Status = report.Status.ToString(),
        Version = report.Version,
        IsCritical = report.IsCritical,
        SignedAtUtc = report.SignedAtUtc,
        FinalizedAtUtc = report.FinalizedAtUtc,
        PdfStorageKey = report.PdfStorageKey
    };
}
