using VetRIS.Domain.Common;

namespace VetRIS.Domain.Entities.Reporting;

public class ReportVersion : BaseEntity
{
    public Guid ReportId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Findings { get; private set; } = default!;
    public string? Impression { get; private set; }
    public string CreatedByUserId { get; private set; } = default!;

    private ReportVersion()
    {
    } // EF Core

    public ReportVersion(Guid reportId, int versionNumber, string findings, string? impression, string createdByUserId)
    {
        ReportId = reportId;
        VersionNumber = versionNumber;
        Findings = findings;
        Impression = impression;
        CreatedByUserId = createdByUserId;
    }
}
