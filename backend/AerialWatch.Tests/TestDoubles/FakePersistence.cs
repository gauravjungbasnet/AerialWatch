using AerialWatch.Application.Abstractions;
using AerialWatch.Core.Entities;

namespace AerialWatch.Tests;

// Test doubles for the repository interfaces so service tests run without SQL Server.
// The application itself only uses SQL Server (AerialWatch.Infrastructure); see SqlServerIntegrationTests for real-database tests.

internal sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
}

/// <summary>Records adds/removes and only "commits" them when the fake unit of work saves, like EF change tracking.</summary>
internal sealed class FakeDatabase : IUnitOfWork
{
    public List<Survey> Surveys { get; } = new();
    public List<ModelCoefficientSet> CoefficientSets { get; } = new();
    private readonly List<Action> _pending = new();

    public void Enqueue(Action change) => _pending.Add(change);

    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCalls++;
        var n = _pending.Count;
        foreach (var change in _pending) change();
        _pending.Clear();
        return Task.FromResult(n);
    }
}

internal abstract class FakeRepository<T> : IRepository<T> where T : EntityBase
{
    protected readonly FakeDatabase Db;
    protected FakeRepository(FakeDatabase db) => Db = db;
    protected abstract List<T> Rows { get; }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Rows.FirstOrDefault(r => r.Id == id));
    public Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<T>>(Rows.ToList());
    public Task AddAsync(T entity, CancellationToken ct = default) { Db.Enqueue(() => Rows.Add(entity)); return Task.CompletedTask; }
    public void Update(T entity) { }
    public void Remove(T entity) => Db.Enqueue(() => Rows.Remove(entity));
}

internal sealed class FakeSurveyRepository : FakeRepository<Survey>, ISurveyRepository
{
    public FakeSurveyRepository(FakeDatabase db) : base(db) { }
    protected override List<Survey> Rows => Db.Surveys;

    public Task<IReadOnlyList<SurveyListItem>> ListWithImageCountsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SurveyListItem>>(Rows.OrderByDescending(s => s.CreatedAt).Select(s => new SurveyListItem(s, s.Images.Count)).ToList());

    public Task<int> CountImagesAsync(Guid surveyId, CancellationToken ct = default) =>
        Task.FromResult(Rows.FirstOrDefault(s => s.Id == surveyId)?.Images.Count ?? 0);
}

internal sealed class FakeModelCoefficientSetRepository : FakeRepository<ModelCoefficientSet>, IModelCoefficientSetRepository
{
    public FakeModelCoefficientSetRepository(FakeDatabase db) : base(db) { }
    protected override List<ModelCoefficientSet> Rows => Db.CoefficientSets;

    public Task<ModelCoefficientSet?> GetLatestAsync(CancellationToken ct = default) =>
        Task.FromResult(Rows.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault());

    public Task<IReadOnlyList<ModelCoefficientSet>> ListHistoryAsync(int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ModelCoefficientSet>>(Rows.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).Take(take).ToList());
}
