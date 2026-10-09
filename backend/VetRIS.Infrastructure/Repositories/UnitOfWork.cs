using VetRIS.Domain.Repositories;
using VetRIS.Infrastructure.Persistence;

namespace VetRIS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly VetRISDbContext _context;

    public UnitOfWork(VetRISDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
