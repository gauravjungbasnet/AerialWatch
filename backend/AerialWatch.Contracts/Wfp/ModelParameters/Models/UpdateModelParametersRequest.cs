namespace AerialWatch.Contracts.Wfp.ModelParameters.Models;

/// <summary>A value for every coefficient type (see GET modelParameters/get for the codes).</summary>
public sealed record UpdateModelParametersRequest(IReadOnlyList<CoefficientEntry>? Values, string? UpdatedBy = null);
