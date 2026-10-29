using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Abstractions;
using AerialWatch.Domain.Entities;

namespace AerialWatch.Persistence.Repositories;

/// <summary>Generic EF Core repository. Specific repositories add query methods on top. Nothing is saved until IUnitOfWork.SaveChangesAsync.</summary>
public class EfRepository<T> : IRepository<T> where T : EntityBase
{
    protected readonly AerialWatchDbContext Db;
    protected readonly DbSet<T> Set;

    public EfRepository(AerialWatchDbContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);
}
