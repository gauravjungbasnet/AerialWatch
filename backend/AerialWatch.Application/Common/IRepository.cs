using AerialWatch.Domain.Common;

namespace AerialWatch.Application.Common;

/// <summary>Generic repository for aggregate roots. Implementations live in the Infrastructure (data) layer.</summary>
public interface IRepository<T> where T : EntityBase
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}
