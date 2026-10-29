namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>
/// The coefficient types of the Hvala et al. (2023) models, in the order of the paper's Tables 1 and 2. The seed migration
/// writes exactly these rows to wfp.CoefficientTypes (checked by MigrationsTests). The published RGB environment model
/// has no temperature term, so there is no type for it.
/// </summary>
public static class CoefficientCatalog
{
    public static readonly IReadOnlyList<CoefficientType> All = Build();

    private static IReadOnlyList<CoefficientType> Build()
    {
        var list = new List<CoefficientType>();

        void Add(CameraType camera, DetectionModelKind model, CoefficientTerm term, string label, string description)
        {
            var id = list.Count + 1;
            list.Add(new CoefficientType
            {
                Id = id,
                Code = CoefficientType.CodeFor(camera, model, term),
                Camera = camera,
                Model = model,
                Term = term,
                Label = label,
                Description = description,
                SortOrder = id * 10,
            });
        }

        foreach (var camera in new[] { CameraType.Thermal, CameraType.Rgb })
        {
            Add(camera, DetectionModelKind.TimeSeason, CoefficientTerm.Intercept, "Baseline", "Early evening, wet season");
            Add(camera, DetectionModelKind.TimeSeason, CoefficientTerm.SeasonDry, "Dry season", "Added in the dry season");
            Add(camera, DetectionModelKind.TimeSeason, CoefficientTerm.MidAfternoon, "Mid-afternoon", "Compared with early evening");
            Add(camera, DetectionModelKind.TimeSeason, CoefficientTerm.MidMorning, "Mid-morning", "Compared with early evening");
            Add(camera, DetectionModelKind.TimeSeason, CoefficientTerm.EarlyMorning, "Early morning", "Compared with early evening");
        }

        Add(CameraType.Thermal, DetectionModelKind.Environment, CoefficientTerm.Intercept, "Baseline", "Open ground");
        Add(CameraType.Thermal, DetectionModelKind.Environment, CoefficientTerm.AmbientTemperatureC, "Air temperature", "Per °C");
        Add(CameraType.Thermal, DetectionModelKind.Environment, CoefficientTerm.TimeUnderCanopy, "Time under canopy", "Per unit, 0 to 1");
        Add(CameraType.Rgb, DetectionModelKind.Environment, CoefficientTerm.Intercept, "Baseline", "Open ground");
        Add(CameraType.Rgb, DetectionModelKind.Environment, CoefficientTerm.TimeUnderCanopy, "Time under canopy", "Per unit, 0 to 1");

        return list;
    }
}
