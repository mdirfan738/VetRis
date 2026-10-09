using VetRIS.Domain.Common;

namespace VetRIS.Domain.Repositories;

/// <summary>
/// Generic persistence contract. Kept in Domain (rather than a not-yet-built
/// Application layer) since it has no framework dependency — Infrastructure
/// provides the EF Core implementation next; Application/API consume the
/// interface once they exist. Constrained to BaseEntity rather than
/// AggregateRoot so it also covers standalone entities that don't currently
/// raise domain events (Hospital, Clinic, Patient, ...); repositories should
/// still only be requested for aggregate/entry-point types (Study, Report,
/// Invoice, Payment, Hospital, Clinic, Patient, Radiologist, ...) — never for
/// child entities reached through a parent's navigation property (Series,
/// StudyComment, InvoiceLineItem, ReportVersion, JobExecutionLog).
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Escape hatch for filtered/paged/sorted list queries that ListAllAsync
    /// can't express. Defaults to a no-tracking query since callers using
    /// this are reading, not editing — pass asNoTracking: false only if you
    /// intend to mutate and Update() the results.
    /// </summary>
    IQueryable<T> GetQueryable(bool asNoTracking = true);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
