namespace AerialWatch.Application.Wfp.ModelParameters;

/// <summary>Reads the coefficient set in use (newest set in wfp.ModelCoefficientSets).</summary>
public interface IActiveModelParameters
{
    Task<ModelParametersState> GetAsync(CancellationToken ct = default);
}
