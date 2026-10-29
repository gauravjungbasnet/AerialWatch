namespace AerialWatch.Domain.Wfp.ResearchModel;

/// <summary>Camera used on the drone. The paper fitted separate models for thermal infrared and visual (RGB).</summary>
public enum CameraType
{
    Thermal,
    Rgb
}
