using Microsoft.EntityFrameworkCore;
using VetRIS.Domain.Common;
using VetRIS.Domain.Repositories;
using VetRIS.Infrastructure.Persistence;

namespace VetRIS.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly DbSet<T> _set;

    public Repository(VetRISDbContext context)
    {
        _set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _set.ToListAsync(cancellationToken);

    public IQueryable<T> GetQueryable(bool asNoTracking = true) =>
        asNoTracking ? _set.AsNoTracking() : _set;

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);
}
