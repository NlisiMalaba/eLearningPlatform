using EduZim.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Infrastructure.Persistence;

public sealed class Repository<T> : IRepository<T>
    where T : class
{
    private readonly EduZimDbContext _context;

    public Repository(EduZimDbContext context)
    {
        _context = context;
    }

    private DbSet<T> Set => _context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await Set.FindAsync([id], ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
    {
        return await Set.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity, ct).ConfigureAwait(false);
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Update(entity);
    }

    public void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
    }
}
