using AerialWatch.Domain.Common;

namespace AerialWatch.Application.Wfp.ModelParameters;

public static class ModelParameterNotes
{
    /// <summary>Warning shown on every result while edited (non-published) coefficients are active.</summary>
    public static IEnumerable<Assumption> For(ModelParametersState state)
    {
        if (!state.IsPublished)
            yield return Assumption.Warning(
                $"Using edited model coefficients ({state.Parameters.Version}), not the published values from Hvala et al. (2023).");
    }
}
