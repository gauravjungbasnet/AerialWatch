using System.Globalization;
using AerialWatch.Application.Common;
using AerialWatch.Contracts.Wfp.ModelParameters.Models;
using AerialWatch.Contracts.Wfp.ModelParameters.Services;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.ModelParameters;

/// <summary>
/// Admin use cases for viewing and editing the detection-model coefficients. The list of coefficients comes from
/// wfp.CoefficientTypes; every save or reset adds a set to wfp.ModelCoefficientSets with one row per coefficient in
/// wfp.ModelCoefficientValues. The newest set is the one in use.
/// </summary>
public sealed class ModelParametersService : IModelParametersService
{
    /// <summary>Logit coefficients outside this range are almost certainly typos.</summary>
    public const double Limit = 20;

    public const int MaxHistory = 100;

    private readonly IActiveModelParameters _active;
    private readonly IModelCoefficientSetRepository _sets;
    private readonly ICoefficientTypeRepository _types;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ModelParametersService(
        IActiveModelParameters active,
        IModelCoefficientSetRepository sets,
        ICoefficientTypeRepository types,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _active = active;
        _sets = sets;
        _types = types;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ModelParametersResponse> GetAsync(CancellationToken ct = default) =>
        ToResponse(await _active.GetAsync(ct), await _types.ListAsync(ct));

    public async Task<ModelParametersResponse> UpdateAsync(
        UpdateModelParametersRequest request, string? updatedBy = null, CancellationToken ct = default)
    {
        var types = await _types.ListAsync(ct);
        var values = Validate(request.Values, types);

        var now = _clock.UtcNow;
        var candidate = DetectionModelParameters.FromValues("", types.Select(t => new CoefficientValue(t.Camera, t.Model, t.Term, values[t.Code])));
        var version = candidate.MatchesPublished()
            ? Hvala2023Coefficients.ModelVersion
            : $"custom-{now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}";

        return await SaveAsync(candidate with { Version = version }, types, now, Clean(updatedBy) ?? Clean(request.UpdatedBy), ct);
    }

    public async Task<ModelParametersResponse> ResetToPublishedAsync(string? updatedBy = null, CancellationToken ct = default) =>
        await SaveAsync(Hvala2023Coefficients.Published, await _types.ListAsync(ct), _clock.UtcNow, Clean(updatedBy), ct);

    public async Task<IReadOnlyList<ModelParametersHistoryItem>> GetHistoryAsync(int take = 20, CancellationToken ct = default)
    {
        var sets = await _sets.ListHistoryAsync(Math.Clamp(take, 1, MaxHistory), ct);
        return sets.Select(s => new ModelParametersHistoryItem(
                s.Id, s.Version, s.IsPublished, s.CreatedAt, s.CreatedBy,
                s.Values.OrderBy(v => v.CoefficientType?.SortOrder ?? v.CoefficientTypeId)
                    .Select(v => new CoefficientEntry(v.CoefficientType?.Code ?? v.CoefficientTypeId.ToString(CultureInfo.InvariantCulture), v.Value))
                    .ToList()))
            .ToList();
    }

    private async Task<ModelParametersResponse> SaveAsync(
        DetectionModelParameters parameters, IReadOnlyList<CoefficientType> types, DateTimeOffset now, string? by, CancellationToken ct)
    {
        var set = ModelCoefficientSet.From(parameters, types, now, by);
        await _sets.AddAsync(set, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToResponse(new ModelParametersState(parameters, set.IsPublished, set.CreatedAt, set.CreatedBy), types);
    }

    // ---- validation: exactly one valid number for every coefficient type ----

    private static Dictionary<string, double> Validate(IReadOnlyList<CoefficientEntry>? entries, IReadOnlyList<CoefficientType> types)
    {
        var errors = new ValidationErrors();
        if (entries is null)
        {
            errors.Add("values", "Values are required.");
            errors.ThrowIfAny();
        }

        var known = types.Select(t => t.Code).ToHashSet(StringComparer.Ordinal);
        var values = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var entry in entries!)
        {
            var code = entry.Code ?? "";
            if (!known.Contains(code)) errors.Add(code, "Unknown coefficient.");
            else if (!values.TryAdd(code, entry.Value)) errors.Add(code, "Given more than once.");
            else if (double.IsNaN(entry.Value) || double.IsInfinity(entry.Value)) errors.Add(code, "Must be a number.");
            else if (Math.Abs(entry.Value) > Limit) errors.Add(code, $"Must be between -{Limit} and {Limit}.");
        }

        foreach (var type in types.Where(t => !values.ContainsKey(t.Code) && !errors.Has(t.Code)))
            errors.Add(type.Code, "A value is required.");

        errors.ThrowIfAny();
        return values;
    }

    private static string? Clean(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, ModelCoefficientSet.CreatedByMaxLength)];

    // ---- mapping ----

    private static ModelParametersResponse ToResponse(ModelParametersState state, IReadOnlyList<CoefficientType> types) => new(
        state.Parameters.Version,
        state.IsPublished,
        state.UpdatedAt,
        state.UpdatedBy,
        types.OrderBy(t => t.SortOrder)
            .Select(t => new ModelCoefficient(
                t.Code, t.Camera, t.Model, t.Term, t.Label, t.Description, t.SortOrder,
                Published: Hvala2023Coefficients.Published.ValueOf(t.Camera, t.Model, t.Term) ?? 0,
                Current: state.Parameters.ValueOf(t.Camera, t.Model, t.Term) ?? 0))
            .ToList(),
        Hvala2023Coefficients.Citation);
}
