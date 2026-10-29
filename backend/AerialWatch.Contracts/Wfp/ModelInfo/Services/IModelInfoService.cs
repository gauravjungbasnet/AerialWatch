using AerialWatch.Contracts.Wfp.ModelInfo.Models;

namespace AerialWatch.Contracts.Wfp.ModelInfo.Services;

public interface IModelInfoService
{
    Task<ModelInfoResponse> GetAsync(CancellationToken ct = default);
}
