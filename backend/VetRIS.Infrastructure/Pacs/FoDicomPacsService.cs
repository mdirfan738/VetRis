using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VetRIS.Application.Common.Interfaces;

namespace VetRIS.Infrastructure.Pacs;

/// <summary>
/// fo-dicom-based implementation of the four DIMSE operations the spec
/// calls out (C-ECHO/C-STORE/C-FIND/C-MOVE).
///
/// HONEST CAVEAT: every other file in this solution builds on stable,
/// extremely well-documented APIs (EF Core, ASP.NET Core, MediatR) I have
/// high confidence in verbatim. fo-dicom's client/request API is far less
/// standardized across versions, and this file could not be checked
/// against an actual compiler or the installed package version. Treat the
/// *shape* (one IPacsService, four DIMSE methods, options-bound
/// host/port/AE titles) as solid, and treat exact method/property names
/// below (DicomClientFactory.Create, DicomCFindRequest.CreateStudyQuery,
/// the OnResponseReceived callback signature) as "verify against fo-dicom's
/// own samples for your installed version" rather than "known correct."
/// </summary>
public class FoDicomPacsService : IPacsService
{
    private readonly PacsOptions _options;
    private readonly ILogger<FoDicomPacsService> _logger;

    public FoDicomPacsService(IOptions<PacsOptions> options, ILogger<FoDicomPacsService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> VerifyConnectionAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var succeeded = false;

        var request = new DicomCEchoRequest
        {
            OnResponseReceived = (req, response) => succeeded = response.Status == DicomStatus.Success
        };

        await client.AddRequestAsync(request);
        await client.SendAsync(cancellationToken);

        _logger.LogInformation("PACS C-ECHO to {Host}:{Port} {Result}", _options.Host, _options.Port, succeeded ? "succeeded" : "failed");
        return succeeded;
    }

    public async Task StoreAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();

        var request = new DicomCStoreRequest(filePath)
        {
            OnResponseReceived = (req, response) =>
            {
                if (response.Status != DicomStatus.Success)
                {
                    _logger.LogWarning("C-STORE to {Host}:{Port} returned {Status} for {FilePath}", _options.Host, _options.Port, response.Status, filePath);
                }
            }
        };

        await client.AddRequestAsync(request);
        await client.SendAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PacsStudyQueryResult>> FindStudiesAsync(
        string? patientId = null, string? accessionNumber = null, string? studyDate = null, CancellationToken cancellationToken = default)
    {
        var results = new List<PacsStudyQueryResult>();
        var client = CreateClient();

        var request = DicomCFindRequest.CreateStudyQuery();
        if (!string.IsNullOrWhiteSpace(patientId)) request.Dataset.AddOrUpdate(DicomTag.PatientID, patientId);
        if (!string.IsNullOrWhiteSpace(accessionNumber)) request.Dataset.AddOrUpdate(DicomTag.AccessionNumber, accessionNumber);
        if (!string.IsNullOrWhiteSpace(studyDate)) request.Dataset.AddOrUpdate(DicomTag.StudyDate, studyDate);

        request.OnResponseReceived = (req, response) =>
        {
            if (response.Status == DicomStatus.Pending && response.Dataset is not null)
            {
                results.Add(new PacsStudyQueryResult(
                    response.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                    response.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, (string?)null),
                    response.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, (string?)null),
                    response.Dataset.GetSingleValueOrDefault(DicomTag.StudyDate, (string?)null)));
            }
        };

        await client.AddRequestAsync(request);
        await client.SendAsync(cancellationToken);

        return results;
    }

    public async Task<bool> RetrieveStudyAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var succeeded = false;

        var request = new DicomCMoveRequest(_options.DestinationAeTitle, studyInstanceUid)
        {
            OnResponseReceived = (req, response) => succeeded = response.Status == DicomStatus.Success
        };

        await client.AddRequestAsync(request);
        await client.SendAsync(cancellationToken);

        return succeeded;
    }

    private IDicomClient CreateClient() =>
        DicomClientFactory.Create(_options.Host, _options.Port, useTls: false, _options.CallingAeTitle, _options.CalledAeTitle);
}