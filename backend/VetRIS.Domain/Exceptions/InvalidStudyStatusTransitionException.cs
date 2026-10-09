using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Exceptions;

public class InvalidStudyStatusTransitionException : DomainException
{
    public StudyStatus FromStatus { get; }
    public StudyStatus ToStatus { get; }

    public InvalidStudyStatusTransitionException(StudyStatus fromStatus, StudyStatus toStatus)
        : base($"Cannot transition a study from '{fromStatus}' to '{toStatus}'.")
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }
}
