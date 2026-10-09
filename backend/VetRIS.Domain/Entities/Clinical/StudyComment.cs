using VetRIS.Domain.Common;

namespace VetRIS.Domain.Entities.Clinical;

public class StudyComment : BaseEntity
{
    public Guid StudyId { get; private set; }
    public string UserId { get; private set; } = default!;
    public string Comment { get; private set; } = default!;

    private StudyComment()
    {
    } // EF Core

    public StudyComment(Guid studyId, string userId, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment)) throw new ArgumentException("Comment text is required.", nameof(comment));

        StudyId = studyId;
        UserId = userId;
        Comment = comment;
    }
}
