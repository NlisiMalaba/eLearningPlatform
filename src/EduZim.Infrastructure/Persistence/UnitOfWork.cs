using EduZim.Application.Common.Interfaces;

namespace EduZim.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly EduZimDbContext _context;

    public UnitOfWork(EduZimDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
