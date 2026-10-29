namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>One coefficient value inside a saved set (table wfp.ModelCoefficientValues). Key: (SetId, CoefficientTypeId).</summary>
public class ModelCoefficientValue
{
    public Guid SetId { get; set; }
    public int CoefficientTypeId { get; set; }

    /// <summary>Loaded with the set when read from the database; not set when saving (only the id is needed).</summary>
    public CoefficientType? CoefficientType { get; set; }

    /// <summary>Logit coefficient.</summary>
    public double Value { get; set; }
}
