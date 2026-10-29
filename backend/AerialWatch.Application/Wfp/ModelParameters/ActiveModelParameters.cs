using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.ModelParameters;

/// <summary>
/// Scoped (one per request): reads the database once per request and reuses the result. Falls back to the
/// published coefficients if the table is empty (the seed migration normally adds the published set).
/// </summary>
public sealed class ActiveModelParameters : IActiveModelParameters
{
    private readonly IModelCoefficientSetRepository _sets;
    private ModelParametersState? _cached;

    public ActiveModelParameters(IModelCoefficientSetRepository sets) => _sets = sets;

    public async Task<ModelParametersState> GetAsync(CancellationToken ct = default)
    {
        if (_cached is not null) return _cached;
        var latest = await _sets.GetLatestAsync(ct);
        _cached = latest is null
            ? new ModelParametersState(Hvala2023Coefficients.Published, true, DateTimeOffset.UnixEpoch, null)
            : new ModelParametersState(latest.ToParameters(), latest.IsPublished, latest.CreatedAt, latest.CreatedBy);
        return _cached;
    }
}
