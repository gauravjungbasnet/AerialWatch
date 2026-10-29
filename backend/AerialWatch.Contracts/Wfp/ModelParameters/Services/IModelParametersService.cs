using AerialWatch.Contracts.Wfp.ModelParameters.Models;

namespace AerialWatch.Contracts.Wfp.ModelParameters.Services;

public interface IModelParametersService
{
    Task<ModelParametersResponse> GetAsync(CancellationToken ct = default);

    /// <param name="updatedBy">Signed-in user name; falls back to request.UpdatedBy.</param>
    Task<ModelParametersResponse> UpdateAsync(UpdateModelParametersRequest request, string? updatedBy = null, CancellationToken ct = default);

    Task<ModelParametersResponse> ResetToPublishedAsync(string? updatedBy = null, CancellationToken ct = default);

    Task<IReadOnlyList<ModelParametersHistoryItem>> GetHistoryAsync(int take = 20, CancellationToken ct = default);
}
