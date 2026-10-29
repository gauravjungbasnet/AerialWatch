using Xunit;
using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Contracts.Wfp.SurveyImages.Models;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.SurveyImages;

/// <summary>Thermal + colour twins (D-039): naming rules, camera per photo, GPS copied to the counted photo.</summary>
public class ImagePairingTests
{
    [Theory]
    [InlineData("IX-12-37642_0032_0023_THM.JPG", CameraType.Thermal, "ix-12-37642_0032_0023")]   // senseFly Duet T
    [InlineData("IX-12-37642_0032_0023.JPG", null, "ix-12-37642_0032_0023")]
    [InlineData("DJI_20240301_0007_T.JPG", CameraType.Thermal, "dji_20240301_0007")]            // DJI
    [InlineData("DJI_20240301_0007_W.JPG", CameraType.Rgb, "dji_20240301_0007")]
    [InlineData("DJI_20240301_0007_V.jpg", CameraType.Rgb, "dji_20240301_0007")]
    [InlineData("frame_0042.png", null, "frame_0042")]
    [InlineData("_T.jpg", null, "_t")]                                                             // suffix alone isn't a twin name
    public void Classify_reads_camera_suffix_and_pair_key(string file, CameraType? camera, string key)
    {
        var (c, k) = ImagePairing.Classify(file);
        Assert.Equal(camera, c);
        Assert.Equal(key, k);
    }

    private static SurveyImage Photo(Survey s, string name, double? lat = null, bool reviewed = false)
    {
        var (camera, key) = ImagePairing.Classify(name);
        return new SurveyImage { SurveyId = s.Id, FileName = name, Camera = camera ?? s.Camera, PairKey = key, Latitude = lat, Longitude = lat is null ? null : 132.3, Reviewed = reviewed };
    }

    [Fact]
    public void Unmarked_twin_of_a_marked_photo_is_the_other_camera_and_lends_its_gps()
    {
        var survey = new Survey { Camera = CameraType.Thermal };
        var thermal = Photo(survey, "A_0001_THM.JPG");
        var colour = Photo(survey, "A_0001.JPG", lat: -14.47);
        var single = Photo(survey, "B_0002.JPG");

        ImagePairing.Resolve(survey, new[] { thermal, colour, single });

        Assert.Equal(CameraType.Thermal, thermal.Camera);
        Assert.Equal(CameraType.Rgb, colour.Camera);          // eBee colour file has no suffix
        Assert.Equal(CameraType.Rgb, single.Camera);          // no twin, but the survey marks only thermal: colour
        Assert.Equal(-14.47, thermal.Latitude);
        Assert.True(thermal.LocationFromTwin);
        Assert.False(colour.LocationFromTwin);
        Assert.Equal(colour.Id, ImagePairing.Twins(new[] { thermal, colour, single })[thermal.Id].Id);
    }

    [Fact]
    public void Rgb_survey_counts_the_colour_photo_and_keeps_the_thermal_one_as_reference()
    {
        var survey = new Survey { Camera = CameraType.Rgb };
        var thermal = Photo(survey, "A_0001_THM.JPG", lat: -14.47);
        var colour = Photo(survey, "A_0001.JPG");

        ImagePairing.Resolve(survey, new[] { thermal, colour });

        Assert.True(ImagePairing.IsCounted(survey, colour));
        Assert.False(ImagePairing.IsCounted(survey, thermal));
        Assert.True(colour.LocationFromTwin);
    }

    [Fact]
    public void Unmarked_files_take_the_survey_camera_when_nothing_or_both_cameras_are_marked()
    {
        var survey = new Survey { Camera = CameraType.Thermal };
        var plainOnly = new[] { Photo(survey, "frame_0001.png"), Photo(survey, "frame_0002.png") };
        ImagePairing.Resolve(survey, plainOnly);
        Assert.All(plainOnly, p => Assert.Equal(CameraType.Thermal, p.Camera));   // single-camera survey: unchanged

        var dji = new[] { Photo(survey, "DJI_0007_T.JPG"), Photo(survey, "DJI_0007_W.JPG"), Photo(survey, "DJI_0008.JPG") };
        ImagePairing.Resolve(survey, dji);
        Assert.Equal(CameraType.Thermal, dji[2].Camera);                            // both marked: survey's camera
    }

    [Fact]
    public void Checked_photos_never_change_camera()
    {
        var survey = new Survey { Camera = CameraType.Thermal };
        var checkedEarlier = Photo(survey, "A_0001.JPG", reviewed: true);   // reviewed as thermal before its twin arrived
        var thermal = Photo(survey, "A_0001_THM.JPG");

        ImagePairing.Resolve(survey, new[] { checkedEarlier, thermal });

        Assert.Equal(CameraType.Thermal, checkedEarlier.Camera);
    }
}

/// <summary>Upload with twins, end to end through SurveyImageService (in-memory fakes).</summary>
public class SurveyImagePairUploadTests
{
    private readonly FakeDatabase _db = new();
    private readonly FakeImageProcessor _processor = new();
    private readonly SurveyImageService _service;
    private readonly Survey _survey = new() { Name = "Pairs", SiteName = "Katherine", Camera = CameraType.Thermal, Status = SurveyStatus.Draft };

    public SurveyImagePairUploadTests()
    {
        _service = new SurveyImageService(new FakeSurveyRepository(_db), new FakeSurveyImageRepository(_db), _db,
            new FakeFileStorage(), _processor, new FixedClock());
        _db.Surveys.Add(_survey);
    }

    private static UploadedFile File(string name)
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.Concat(System.Text.Encoding.ASCII.GetBytes(name)).ToArray();
        return new UploadedFile(name, bytes.Length, new MemoryStream(bytes));
    }

    private readonly ImageMetadata _noGps = new(640, 512, new DateTime(2021, 2, 13, 18, 2, 0), null, null, null);

    [Fact]
    public async Task Colour_twin_upload_is_a_reference_and_lends_its_gps_to_the_thermal_photo()
    {
        _processor.Metadata = _noGps;
        var thermalResponse = await _service.UploadAsync(_survey.Id, new[] { File("X_0001_THM.JPG") });
        Assert.Contains("No GPS", thermalResponse.Files[0].Message);

        _processor.Metadata = new ImageMetadata(5472, 3648, new DateTime(2021, 2, 13, 18, 2, 0), null, -14.47, 132.3);
        var colourResponse = await _service.UploadAsync(_survey.Id, new[] { File("X_0001.JPG") });

        var colour = colourResponse.Files[0];
        Assert.Equal(UploadFileStatus.Uploaded, colour.Status);
        Assert.True(colour.Image!.IsReference);
        Assert.Equal(CameraType.Rgb, colour.Image.Camera);
        Assert.Contains("reference photo", colour.Message);
        Assert.Contains("Paired with X_0001_THM.JPG", colour.Message);

        var thermal = _db.Images.Single(i => i.FileName == "X_0001_THM.JPG");
        Assert.Equal(-14.47, thermal.Latitude);                    // copied from the colour twin on the second upload
        Assert.True(thermal.LocationFromTwin);

        var list = await _service.ListAsync(_survey.Id);
        Assert.Equal(colour.Image.Id, list.Single(i => i.FileName == "X_0001_THM.JPG").TwinId);
        Assert.False(list.Single(i => i.FileName == "X_0001_THM.JPG").IsReference);
    }

    [Fact]
    public async Task Colour_uploaded_first_is_reclassified_when_its_thermal_twin_arrives()
    {
        await _service.UploadAsync(_survey.Id, new[] { File("Y_0002.JPG") });      // unmarked: thermal survey's camera for now
        Assert.Equal(CameraType.Thermal, _db.Images.Single().Camera);

        await _service.UploadAsync(_survey.Id, new[] { File("Y_0002_THM.JPG") });

        Assert.Equal(CameraType.Rgb, _db.Images.Single(i => i.FileName == "Y_0002.JPG").Camera);
        Assert.Equal(CameraType.Thermal, _db.Images.Single(i => i.FileName == "Y_0002_THM.JPG").Camera);
    }

    [Fact]
    public async Task During_review_reference_photos_can_be_added_but_counted_ones_are_refused()
    {
        await _service.UploadAsync(_survey.Id, new[] { File("Z_0003_THM.JPG") });
        _survey.Status = SurveyStatus.InReview;

        var response = await _service.UploadAsync(_survey.Id, new[] { File("Z_0003.JPG"), File("NEW_0009_THM.JPG") });

        Assert.Equal(UploadFileStatus.Uploaded, response.Files.Single(f => f.FileName == "Z_0003.JPG").Status);
        var refused = response.Files.Single(f => f.FileName == "NEW_0009_THM.JPG");
        Assert.Equal(UploadFileStatus.Rejected, refused.Status);
        Assert.Contains("review has started", refused.Message);
        Assert.DoesNotContain(_db.Images, i => i.FileName == "NEW_0009_THM.JPG");
        Assert.Equal(SurveyStatus.InReview, _survey.Status);
        Assert.Equal(CameraType.Rgb, _db.Images.Single(i => i.FileName == "Z_0003.JPG").Camera);
    }

    [Fact]
    public async Task A_failed_survey_still_refuses_uploads()
    {
        _survey.Status = SurveyStatus.Failed;
        await Assert.ThrowsAsync<AerialWatch.Contracts.Common.RequestValidationException>(() =>
            _service.UploadAsync(_survey.Id, new[] { File("A.JPG") }));
    }
}
