namespace VetRIS.Domain.Repositories;

/// <summary>Commits everything changed through the repositories in this request as one transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
