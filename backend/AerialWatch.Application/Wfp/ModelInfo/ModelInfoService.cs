using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Contracts.Wfp.ModelInfo.Models;
using AerialWatch.Contracts.Wfp.ModelInfo.Services;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.ModelInfo;

/// <summary>Citation, coefficients and assumptions for the About page and "How we calculated this".</summary>
public sealed class ModelInfoService : IModelInfoService
{
    private static readonly string[] OurAssumptions =
    {
        "Season coded 0 = wet, 1 = dry.",
        "Time under canopy is a proportion from 0 to 1.",
        "Point estimates only (published CI column appears misaligned).",
        "Population estimate N = C / p (paper does not print its correction formula).",
        "95% range from detection chance only; excludes coefficient uncertainty.",
        "Clock times are mapped to the nearest study window; night is not supported."
    };

    private readonly IActiveModelParameters _parameters;

    public ModelInfoService(IActiveModelParameters parameters) => _parameters = parameters;

    public async Task<ModelInfoResponse> GetAsync(CancellationToken ct = default)
    {
        var p = (await _parameters.GetAsync(ct)).Parameters;
        return new(
        p.Version,
        Hvala2023Coefficients.Citation,
        Hvala2023Coefficients.Doi,
        Hvala2023Coefficients.StudyConditions,
        new
        {
            timeSeason = new
            {
                thermal = p.ThermalTimeSeason,
                rgb = p.RgbTimeSeason,
                referenceWindow = "earlyEvening"
            },
            environment = new
            {
                thermal = p.ThermalEnvironment,
                rgb = p.RgbEnvironment
            }
        },
        OurAssumptions);
    }
}
