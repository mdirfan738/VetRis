using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Exceptions;

public class InvalidReportStatusTransitionException : DomainException
{
    public ReportStatus FromStatus { get; }
    public ReportStatus ToStatus { get; }

    public InvalidReportStatusTransitionException(ReportStatus fromStatus, ReportStatus toStatus)
        : base($"Cannot transition a report from '{fromStatus}' to '{toStatus}'.")
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }
}
