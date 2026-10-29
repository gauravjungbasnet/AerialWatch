using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.SurveyImages;

/// <summary>
/// Thermal + colour twins (D-039). Dual-camera drones save each shot twice with the same name and a camera suffix
/// (senseFly Duet T: "X_THM.JPG" + "X.JPG"; DJI: "X_T.JPG" + "X_W.JPG"/"X_V.JPG"). Photos with the same key are twins.
/// Only photos from the survey's camera are counted; the other camera's photos are reference twins shown beside them.
/// </summary>
public static class ImagePairing
{
    // Upper-case suffixes before the extension. A file without a known suffix is "unmarked".
    private static readonly (string Suffix, CameraType Camera)[] Suffixes =
    {
        ("_THM", CameraType.Thermal), ("_TIR", CameraType.Thermal), ("_IR", CameraType.Thermal), ("_T", CameraType.Thermal),
        ("_RGB", CameraType.Rgb), ("_VIS", CameraType.Rgb), ("_W", CameraType.Rgb), ("_V", CameraType.Rgb), ("_Z", CameraType.Rgb),
    };

    /// <summary>Camera named by the file's suffix (null when unmarked) and the pair key.</summary>
    public static (CameraType? Camera, string Key) Classify(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName.Trim());
        foreach (var (suffix, camera) in Suffixes)
        {
            if (stem.Length > suffix.Length && stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return (camera, Key(stem[..^suffix.Length]));
        }
        return (null, Key(stem));
    }

    private static string Key(string stem)
    {
        var key = stem.ToLowerInvariant();
        return key.Length <= SurveyImage.PairKeyMaxLength ? key : key[..SurveyImage.PairKeyMaxLength];
    }

    public static CameraType Other(CameraType camera) => camera == CameraType.Thermal ? CameraType.Rgb : CameraType.Thermal;

    /// <summary>Plain-language camera name for messages.</summary>
    public static string Label(CameraType camera) => camera == CameraType.Thermal ? "thermal" : "colour (RGB)";

    /// <summary>True for photos that are counted, reviewed, sent to the AI and used for training.</summary>
    public static bool IsCounted(Survey survey, SurveyImage image) => image.Camera == survey.Camera;

    /// <summary>
    /// Sets each photo's camera and copies GPS from twins, for all photos of one survey (edits the objects in place).
    /// Camera: the file suffix if it has one. An unmarked file follows the survey's naming: when the survey's marked files
    /// all name one camera (eBee marks only thermal, "X_THM.JPG", so a plain "X.JPG" is colour), it is the other camera,
    /// with or without a twin; otherwise (no marked files, or both cameras marked as on DJI) it is the survey's camera.
    /// Checked photos never change camera.
    /// GPS: a counted photo without GPS takes its twin's location (LocationFromTwin = true).
    /// </summary>
    public static void Resolve(Survey survey, IEnumerable<SurveyImage> images)
    {
        var all = images.ToList();
        var unmarkedCamera = UnmarkedCamera(survey, all);
        foreach (var m in all)
        {
            var camera = Classify(m.FileName).Camera ?? unmarkedCamera;
            if (!m.Reviewed && m.Camera != camera) m.Camera = camera;
        }

        foreach (var group in all.Where(i => i.PairKey is not null).GroupBy(i => i.PairKey!))
        {
            var members = group.ToList();

            var twinWithGps = members.FirstOrDefault(m => !IsCounted(survey, m) && m.Latitude is not null && m.Longitude is not null);
            if (twinWithGps is null) continue;
            foreach (var m in members.Where(m => IsCounted(survey, m) && (m.Latitude is null || m.Longitude is null)))
            {
                m.Latitude = twinWithGps.Latitude;
                m.Longitude = twinWithGps.Longitude;
                m.LocationFromTwin = true;
            }
        }
    }

    /// <summary>Camera for files without a suffix, from the survey's naming (see <see cref="Resolve"/>).</summary>
    private static CameraType UnmarkedCamera(Survey survey, IReadOnlyList<SurveyImage> images)
    {
        var marked = images.Select(i => Classify(i.FileName).Camera).Where(c => c is not null).Select(c => c!.Value).Distinct().ToList();
        return marked.Count == 1 ? Other(marked[0]) : survey.Camera;
    }

    /// <summary>Twin of each photo (the first photo with the same key from the other camera).</summary>
    public static Dictionary<Guid, SurveyImage> Twins(IEnumerable<SurveyImage> images)
    {
        var map = new Dictionary<Guid, SurveyImage>();
        foreach (var group in images.Where(i => i.PairKey is not null).GroupBy(i => i.PairKey!))
        {
            foreach (var m in group)
            {
                var twin = group.FirstOrDefault(o => o.Id != m.Id && o.Camera != m.Camera);
                if (twin is not null) map[m.Id] = twin;
            }
        }
        return map;
    }
}
