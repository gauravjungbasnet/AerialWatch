using AerialWatch.Contracts.Wfp.Estimates.Models;

namespace AerialWatch.Contracts.Wfp.Estimates.Services;

public interface IEstimateService
{
    Task<EstimateResponse> PreviewAsync(EstimatePreviewRequest request, CancellationToken ct = default);
}
