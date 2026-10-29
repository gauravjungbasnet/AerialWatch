using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.ModelParameters;

/// <summary>Reads wfp.CoefficientTypes (reference data, seeded by migration).</summary>
public interface ICoefficientTypeRepository
{
    /// <summary>All coefficient types, ordered by SortOrder.</summary>
    Task<IReadOnlyList<CoefficientType>> ListAsync(CancellationToken ct = default);
}
