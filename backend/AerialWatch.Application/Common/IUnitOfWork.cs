namespace AerialWatch.Application.Common;

/// <summary>Commits all changes made through repositories as one unit.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
