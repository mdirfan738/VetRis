using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Events;

public sealed record StudyStatusChangedEvent(Guid StudyId, StudyStatus FromStatus, StudyStatus ToStatus) : DomainEventBase;
