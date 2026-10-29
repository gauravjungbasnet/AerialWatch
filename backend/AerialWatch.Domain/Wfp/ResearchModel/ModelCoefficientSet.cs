using AerialWatch.Domain.Common;

namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>
/// One saved version of the detection-model coefficients (table wfp.ModelCoefficientSets) with one value row per
/// coefficient type (wfp.ModelCoefficientValues). Every save or reset adds a set, so the table is an audit history;
/// the newest set is the one in use.
/// </summary>
public class ModelCoefficientSet : EntityBase
{
    public const int VersionMaxLength = 40;
    public const int CreatedByMaxLength = 80;

    public string Version { get; set; } = string.Empty;

    /// <summary>True when every value equals the published Hvala et al. (2023) coefficients.</summary>
    public bool IsPublished { get; set; }

    public string? CreatedBy { get; set; }

    public List<ModelCoefficientValue> Values { get; set; } = new();

    /// <summary>Turns the value rows back into typed parameters. Needs each value's CoefficientType loaded.</summary>
    public DetectionModelParameters ToParameters() =>
        DetectionModelParameters.FromValues(Version, Values.Select(v =>
        {
            var type = v.CoefficientType
                ?? throw new InvalidOperationException($"Coefficient type {v.CoefficientTypeId} was not loaded with set {Version}.");
            return new CoefficientValue(type.Camera, type.Model, type.Term, v.Value);
        }));

    /// <summary>A new set with one value per coefficient type.</summary>
    public static ModelCoefficientSet From(
        DetectionModelParameters parameters, IEnumerable<CoefficientType> types, DateTimeOffset now, string? createdBy)
    {
        var set = new ModelCoefficientSet
        {
            Version = parameters.Version,
            IsPublished = parameters.MatchesPublished(),
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now,
        };
        foreach (var type in types)
        {
            var value = parameters.ValueOf(type.Camera, type.Model, type.Term)
                ?? throw new InvalidOperationException($"Parameters {parameters.Version} have no value for {type.Code}.");
            set.Values.Add(new ModelCoefficientValue { SetId = set.Id, CoefficientTypeId = type.Id, Value = value });
        }
        return set;
    }
}
