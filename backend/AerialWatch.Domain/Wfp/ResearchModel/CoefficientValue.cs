namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>One coefficient identified by camera, model and term, e.g. (Thermal, TimeSeason, Intercept) = 0.951.</summary>
public readonly record struct CoefficientValue(CameraType Camera, DetectionModelKind Model, CoefficientTerm Term, double Value);
