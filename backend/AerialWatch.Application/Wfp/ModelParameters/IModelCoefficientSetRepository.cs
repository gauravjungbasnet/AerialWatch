using AerialWatch.Application.Common;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.ModelParameters;

/// <summary>Saved coefficient sets. Reads always include each set's values and their coefficient types.</summary>
public interface IModelCoefficientSetRepository : IRepository<ModelCoefficientSet>
{
    /// <summary>The newest saved set (the one in use), or null when the table is empty.</summary>
    Task<ModelCoefficientSet?> GetLatestAsync(CancellationToken ct = default);

    /// <summary>Newest first, for the audit history.</summary>
    Task<IReadOnlyList<ModelCoefficientSet>> ListHistoryAsync(int take, CancellationToken ct = default);
}
