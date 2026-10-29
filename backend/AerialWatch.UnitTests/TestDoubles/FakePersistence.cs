using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.Review;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Application.Wfp.TwinCheck;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.UnitTests.TestDoubles;

// Test doubles for the repository interfaces so service tests run without SQL Server.
// The application itself only uses SQL Server (AerialWatch.Infrastructure); see AerialWatch.IntegrationTests for real-database tests.

internal sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
}

/// <summary>Records adds/removes and only "commits" them when the fake unit of work saves, like EF change tracking.</summary>
internal sealed class FakeDatabase : IUnitOfWork
{
    public List<Survey> Surveys { get; } = new();
    public List<SurveyImage> Images { get; } = new();
    public List<AnimalDetection> Detections { get; } = new();
    public List<TwinMark> TwinMarks { get; } = new();
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

    /// <summary>Same rules as the SQL query in SurveyRepository, done with LINQ to Objects.</summary>
    public Task<SurveySearchResult> SearchAsync(SurveySearch q, CancellationToken ct = default)
    {
        int Photos(Survey s) => Db.Images.Count(i => i.SurveyId == s.Id);
        IEnumerable<Survey> rows = Rows;
        if (q.Text is { } t)
            rows = rows.Where(s => s.Name.Contains(t, StringComparison.OrdinalIgnoreCase) || s.SiteName.Contains(t, StringComparison.OrdinalIgnoreCase));
        if (q.Date is { } d) rows = rows.Where(s => s.SurveyDate == d);
        if (q.Camera is { } c) rows = rows.Where(s => s.Camera == c);
        if (q.Status is { } st) rows = rows.Where(s => s.Status == st);
        var matched = rows.ToList();

        Func<Survey, object> key = q.Sort switch
        {
            SurveySortField.Name => s => s.Name,
            SurveySortField.SiteName => s => s.SiteName,
            SurveySortField.SurveyDate => s => s.SurveyDate,
            SurveySortField.Status => s => s.Status.ToString(), // stored as text in SQL
            SurveySortField.ImageCount => s => Photos(s),
            _ => s => s.CreatedAt,
        };
        var ordered = (q.Descending ? matched.OrderByDescending(key) : matched.OrderBy(key))
            .ThenByDescending(s => s.CreatedAt).ThenBy(s => s.Id);
        var page = ordered.Skip(q.Skip).Take(q.Take).Select(s => new SurveyListItem(s, Photos(s))).ToList();
        return Task.FromResult(new SurveySearchResult(page, matched.Count));
    }

    public Task<SurveyStats> GetStatsAsync(CancellationToken ct = default) =>
        Task.FromResult(new SurveyStats(Rows.Count, Rows.Count(s => s.Status == SurveyStatus.InReview), Db.Images.Count));

    public Task<int> CountImagesAsync(Guid surveyId, CancellationToken ct = default) =>
        Task.FromResult(Db.Images.Count(i => i.SurveyId == surveyId));
}

internal sealed class FakeSurveyImageRepository : FakeRepository<SurveyImage>, ISurveyImageRepository
{
    public FakeSurveyImageRepository(FakeDatabase db) : base(db) { }
    protected override List<SurveyImage> Rows => Db.Images;

    public Task<IReadOnlyList<SurveyImage>> ListBySurveyAsync(Guid surveyId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SurveyImage>>(Rows.Where(i => i.SurveyId == surveyId)
            .OrderBy(i => i.TakenAt == null).ThenBy(i => i.TakenAt).ThenBy(i => i.FileName).ToList());

    public Task<IReadOnlyList<SurveyImage>> ListByPairKeyAsync(Guid surveyId, string pairKey, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SurveyImage>>(Rows.Where(i => i.SurveyId == surveyId && i.PairKey == pairKey).ToList());

    public Task<IReadOnlyCollection<string>> ListHashesAsync(Guid surveyId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyCollection<string>>(Rows.Where(i => i.SurveyId == surveyId).Select(i => i.Sha256).ToList());
}

/// <summary>Same queries as DetectionRepository, over the in-memory lists (a box's survey comes from its photo).</summary>
internal sealed class FakeDetectionRepository : FakeRepository<AnimalDetection>, IDetectionRepository
{
    public FakeDetectionRepository(FakeDatabase db) : base(db) { }
    protected override List<AnimalDetection> Rows => Db.Detections;

    private Guid? SurveyOf(AnimalDetection d) => Db.Images.FirstOrDefault(i => i.Id == d.ImageId)?.SurveyId;

    public Task<IReadOnlyList<AnimalDetection>> ListByImageAsync(Guid imageId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AnimalDetection>>(Rows.Where(d => d.ImageId == imageId)
            .OrderBy(d => d.Source.ToString()).ThenByDescending(d => d.Confidence).ThenBy(d => d.CreatedAt).ToList());

    public Task<IReadOnlyList<DetectionTally>> TallyBySurveyAsync(Guid surveyId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DetectionTally>>(Rows.Where(d => SurveyOf(d) == surveyId)
            .GroupBy(d => new { d.ImageId, d.Source, d.Status })
            .Select(g => new DetectionTally(g.Key.ImageId, g.Key.Source, g.Key.Status, g.Count())).ToList());

    public Task<IReadOnlyList<AnimalDetection>> ListPendingAiAsync(Guid surveyId, double minConfidence, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AnimalDetection>>(Rows.Where(d => SurveyOf(d) == surveyId && d.Source == DetectionSource.Ai
            && d.Status == DetectionStatus.Pending && d.Confidence >= minConfidence).ToList());
}

internal sealed class FakeCoefficientTypeRepository : ICoefficientTypeRepository
{
    /// <summary>Same rows the seed migration writes to wfp.CoefficientTypes.</summary>
    public Task<IReadOnlyList<CoefficientType>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CoefficientType>>(CoefficientCatalog.All.OrderBy(t => t.SortOrder).ToList());
}

/// <summary>Like the EF repository, reads return each value with its CoefficientType loaded.</summary>
internal sealed class FakeModelCoefficientSetRepository : FakeRepository<ModelCoefficientSet>, IModelCoefficientSetRepository
{
    public FakeModelCoefficientSetRepository(FakeDatabase db) : base(db) { }
    protected override List<ModelCoefficientSet> Rows => Db.CoefficientSets;

    public Task<ModelCoefficientSet?> GetLatestAsync(CancellationToken ct = default) =>
        Task.FromResult(Newest().Select(WithTypes).FirstOrDefault());

    public Task<IReadOnlyList<ModelCoefficientSet>> ListHistoryAsync(int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ModelCoefficientSet>>(Newest().Take(take).Select(WithTypes).ToList());

    private IEnumerable<ModelCoefficientSet> Newest() => Rows.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);

    private static ModelCoefficientSet WithTypes(ModelCoefficientSet set)
    {
        foreach (var v in set.Values) v.CoefficientType = CoefficientCatalog.All.Single(t => t.Id == v.CoefficientTypeId);
        return set;
    }
}

/// <summary>Same queries as TwinMarkRepository (D-040); fills the Detection navigation like EF's Include.</summary>
internal sealed class FakeTwinMarkRepository : FakeRepository<TwinMark>, ITwinMarkRepository
{
    public FakeTwinMarkRepository(FakeDatabase db) : base(db) { }
    protected override List<TwinMark> Rows => Db.TwinMarks;

    private TwinMark WithDetection(TwinMark m)
    {
        m.Detection = Db.Detections.FirstOrDefault(d => d.Id == m.DetectionId);
        return m;
    }

    public Task<IReadOnlyList<TwinMark>> ListByImageAsync(Guid imageId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TwinMark>>(Rows.Select(WithDetection).Where(m => m.CountedImageId == imageId).ToList());

    public Task<IReadOnlyList<TwinMark>> ListBySurveyAsync(Guid surveyId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TwinMark>>(Rows.Select(WithDetection)
            .Where(m => Db.Images.Any(i => i.Id == m.CountedImageId && i.SurveyId == surveyId)).ToList());

    public Task<TwinMark?> GetByDetectionAsync(Guid detectionId, CancellationToken ct = default) =>
        Task.FromResult(Rows.FirstOrDefault(m => m.DetectionId == detectionId));
}
