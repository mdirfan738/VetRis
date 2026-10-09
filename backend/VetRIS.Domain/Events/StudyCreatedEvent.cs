using VetRIS.Domain.Common;

namespace VetRIS.Domain.Events;

public sealed record StudyCreatedEvent(Guid StudyId, string AccessionNumber, Guid PatientId, Guid ClinicId) : DomainEventBase;
