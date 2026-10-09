using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Clinical;

public class Series : BaseEntity
{
    public Guid StudyId { get; private set; }
    public string SeriesInstanceUid { get; private set; } = default!;
    public int SeriesNumber { get; private set; }
    public Modality Modality { get; private set; }
    public string? BodyPart { get; private set; }
    public string? Description { get; private set; }

    private readonly List<DicomImage> _images = new();
    public IReadOnlyCollection<DicomImage> Images => _images.AsReadOnly();

    private Series()
    {
    } // EF Core

    public Series(Guid studyId, string seriesInstanceUid, int seriesNumber, Modality modality,
        string? bodyPart = null, string? description = null)
    {
        if (studyId == Guid.Empty) throw new ArgumentException("A parent study is required.", nameof(studyId));
        if (string.IsNullOrWhiteSpace(seriesInstanceUid)) throw new ArgumentException("SeriesInstanceUID is required.", nameof(seriesInstanceUid));

        StudyId = studyId;
        SeriesInstanceUid = seriesInstanceUid;
        SeriesNumber = seriesNumber;
        Modality = modality;
        BodyPart = bodyPart;
        Description = description;
    }

    public void AddImage(DicomImage image) => _images.Add(image);
}
