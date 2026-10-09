using VetRIS.Domain.Common;

namespace VetRIS.Domain.Events;

public sealed record ReportFinalizedEvent(Guid ReportId, Guid StudyId, Guid RadiologistId) : DomainEventBase;
