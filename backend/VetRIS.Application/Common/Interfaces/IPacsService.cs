namespace VetRIS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the four DIMSE operations the spec calls out (C-ECHO,
/// C-STORE, C-FIND, C-MOVE) against ERAD PACS. Kept here so workflow-service
/// jobs and command handlers depend on this contract, not on fo-dicom
/// directly — Infrastructure supplies the real implementation.
/// </summary>
public interface IPacsService
{
    /// <summary>C-ECHO — connectivity/health check against the configured PACS node.</summary>
    Task<bool> VerifyConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>C-STORE — pushes a single local DICOM file to PACS.</summary>
    Task StoreAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>C-FIND — study-level query, returns matching studies (not the pixel data, just the query-level attributes).</summary>
    Task<IReadOnlyList<PacsStudyQueryResult>> FindStudiesAsync(
        string? patientId = null, string? accessionNumber = null, string? studyDate = null, CancellationToken cancellationToken = default);

    /// <summary>C-MOVE — asks PACS to send a study to the configured destination AE title.</summary>
    Task<bool> RetrieveStudyAsync(string studyInstanceUid, CancellationToken cancellationToken = default);
}

public record PacsStudyQueryResult(string StudyInstanceUid, string? PatientId, string? AccessionNumber, string? StudyDate);
