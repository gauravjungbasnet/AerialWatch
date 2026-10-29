using AerialWatch.Application.Common;

namespace AerialWatch.Infrastructure.Persistence.Common;

/// <summary>Commits every change tracked by the request's DbContext in one SQL Server transaction.</summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AerialWatchDbContext _db;

    public EfUnitOfWork(AerialWatchDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
