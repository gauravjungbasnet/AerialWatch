using System.Text;
using Xunit;
using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.SurveyImages.Models;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.SurveyImages;

/// <summary>Upload rules from D-030, with in-memory storage and a fake image decoder (no disk, no database).</summary>
public class SurveyImageServiceTests
{
    private readonly FakeDatabase _db = new();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeImageProcessor _processor = new();
    private readonly FixedClock _clock = new();
    private readonly SurveyImageService _service;
    private readonly Survey _survey;

    public SurveyImageServiceTests()
    {
        _service = new SurveyImageService(new FakeSurveyRepository(_db), new FakeSurveyImageRepository(_db), _db,
            _storage, _processor, _clock);
        _survey = new Survey { Name = "North paddock", SiteName = "Katherine", Camera = CameraType.Thermal, Status = SurveyStatus.Draft };
        _db.Surveys.Add(_survey);
    }

    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF, 0xE0 };
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static UploadedFile File(string name, string body = "", byte[]? magic = null, long? declaredLength = null)
    {
        var bytes = (magic ?? JpegMagic).Concat(Encoding.ASCII.GetBytes(body.Length > 0 ? body : name)).ToArray();
        return new UploadedFile(name, declaredLength ?? bytes.Length, new MemoryStream(bytes));
    }

    private Task<UploadImagesResponse> Upload(params UploadedFile[] files) => _service.UploadAsync(_survey.Id, files);

    [Fact]
    public async Task Upload_saves_original_and_thumbnail_and_reads_exif()
    {
        var response = await Upload(File("DJI_0001.JPG"));

        var result = Assert.Single(response.Files);
        Assert.Equal(UploadFileStatus.Uploaded, result.Status);
        Assert.Equal(1, response.Uploaded);
        Assert.Equal(1, response.TotalImages);

        var row = Assert.Single(_db.Images);
        Assert.Equal("DJI_0001.JPG", row.FileName);
        Assert.Equal("image/jpeg", row.ContentType);
        Assert.Equal($"wfp/surveys/{_survey.Id}/originals/{row.Id}.jpg", row.StoragePath); // GUID name, lower-case ext
        Assert.Equal($"wfp/surveys/{_survey.Id}/thumbs/{row.Id}.jpg", row.ThumbnailPath);
        Assert.True(_storage.Files.ContainsKey(row.StoragePath));
        Assert.True(_storage.Files.ContainsKey(row.ThumbnailPath!));
        Assert.Equal(64, row.Sha256.Length);
        Assert.Equal(4000, row.WidthPx);
        Assert.Equal(-14.4652, row.Latitude);
        Assert.Equal(SurveyStatus.Uploading, _survey.Status);
    }

    [Fact]
    public async Task Exif_time_without_zone_is_read_as_NT_time()
    {
        await Upload(File("a.jpg"));
        var takenAt = Assert.Single(_db.Images).TakenAt!.Value;
        Assert.Equal(new TimeSpan(9, 30, 0), takenAt.Offset);
        Assert.Equal(new DateTime(2026, 2, 10, 18, 21, 5), takenAt.DateTime);
    }

    [Fact]
    public async Task Exif_time_zone_is_used_when_the_photo_records_one()
    {
        _processor.Metadata = _processor.Metadata with { TakenUtcOffset = TimeSpan.FromHours(10) };
        await Upload(File("a.jpg"));
        Assert.Equal(TimeSpan.FromHours(10), Assert.Single(_db.Images).TakenAt!.Value.Offset);
    }

    [Fact]
    public async Task Missing_exif_still_uploads_with_warnings()
    {
        _processor.Metadata = new ImageMetadata(640, 480, null, null, null, null);
        var result = Assert.Single((await Upload(File("export.png", magic: PngMagic))).Files);

        Assert.Equal(UploadFileStatus.Uploaded, result.Status);
        Assert.Contains("No capture time", result.Message);
        Assert.Contains("No GPS", result.Message);
    }

    [Theory]
    [InlineData("notes.txt", "JPEG (.jpg, .jpeg) and PNG")]
    [InlineData("tool.exe", "JPEG (.jpg, .jpeg) and PNG")]
    [InlineData("scan.tif", "JPEG (.jpg, .jpeg) and PNG")]
    public async Task Rejects_other_file_types(string name, string message)
    {
        var result = Assert.Single((await Upload(File(name))).Files);
        Assert.Equal(UploadFileStatus.Rejected, result.Status);
        Assert.Contains(message, result.Message);
        Assert.Empty(_db.Images);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Rejects_a_renamed_file_whose_content_is_not_an_image()
    {
        var fake = new UploadedFile("photo.jpg", 4, new MemoryStream(Encoding.ASCII.GetBytes("MZ\x90\x00")));
        var result = Assert.Single((await Upload(fake)).Files);
        Assert.Equal(UploadFileStatus.Rejected, result.Status);
        Assert.Contains("not a JPEG or PNG", result.Message);
    }

    [Fact]
    public async Task Rejects_a_png_named_jpg()
    {
        var result = Assert.Single((await Upload(File("photo.jpg", magic: PngMagic))).Files);
        Assert.Equal(UploadFileStatus.Rejected, result.Status);
        Assert.Contains("doesn't match its extension", result.Message);
    }

    [Fact]
    public async Task Rejects_files_over_50_MB_and_empty_files()
    {
        var response = await Upload(
            File("big.jpg", declaredLength: SurveyImageRules.MaxFileBytes + 1),
            new UploadedFile("empty.jpg", 0, new MemoryStream()));

        Assert.All(response.Files, f => Assert.Equal(UploadFileStatus.Rejected, f.Status));
        Assert.Contains("larger than 50 MB", response.Files[0].Message);
        Assert.Contains("empty", response.Files[1].Message);
    }

    [Fact]
    public async Task Undecodable_image_is_rejected_and_its_file_removed()
    {
        var result = Assert.Single((await Upload(File("broken.jpg", body: "CORRUPT"))).Files);

        Assert.Equal(UploadFileStatus.Rejected, result.Status);
        Assert.Empty(_storage.Files);
        Assert.Empty(_db.Images);
    }

    [Fact]
    public async Task Photo_without_thumbnail_is_kept_with_a_warning()
    {
        var result = Assert.Single((await Upload(File("odd.jpg", body: "NOTHUMB"))).Files);

        Assert.Equal(UploadFileStatus.Uploaded, result.Status);
        Assert.False(result.Image!.HasThumbnail);
        Assert.Contains("No preview", result.Message);
    }

    [Fact]
    public async Task Same_photo_twice_is_skipped_in_a_batch_and_across_batches()
    {
        var first = await Upload(File("DJI_0001.JPG", body: "same"), File("copy of DJI_0001.JPG", body: "same"));
        Assert.Equal(1, first.Uploaded);
        Assert.Equal(1, first.Duplicates);

        var second = await Upload(File("DJI_0001.JPG", body: "same"));
        Assert.Equal(UploadFileStatus.Duplicate, Assert.Single(second.Files).Status);
        Assert.Single(_db.Images);
    }

    [Fact]
    public async Task One_bad_file_does_not_stop_the_others()
    {
        var response = await Upload(File("a.jpg"), File("b.txt"), File("c.jpg"));
        Assert.Equal(2, response.Uploaded);
        Assert.Equal(1, response.Rejected);
        Assert.Equal(2, _db.Images.Count);
    }

    [Fact]
    public async Task Path_in_the_file_name_is_dropped()
    {
        await Upload(File(@"C:\flights\day1\DJI_0007.JPG"));
        Assert.Equal("DJI_0007.JPG", Assert.Single(_db.Images).FileName);
    }

    [Fact]
    public async Task Batch_limits_and_survey_state_are_validated()
    {
        var tooMany = Enumerable.Range(0, SurveyImageRules.MaxFilesPerRequest + 1).Select(i => File($"{i}.jpg")).ToArray();
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => Upload(tooMany));
        Assert.True(ex.Errors.ContainsKey("files"));

        await Assert.ThrowsAsync<RequestValidationException>(() => Upload());

        // During review only reference twins from the other camera may be added (D-040); a counted photo is refused per file.
        _survey.Status = SurveyStatus.InReview;
        var during = Assert.Single((await Upload(File("a.jpg"))).Files);
        Assert.Equal(UploadFileStatus.Rejected, during.Status);

        _survey.Status = SurveyStatus.Failed;
        ex = await Assert.ThrowsAsync<RequestValidationException>(() => Upload(File("a.jpg")));
        Assert.True(ex.Errors.ContainsKey("survey"));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UploadAsync(Guid.NewGuid(), new[] { File("a.jpg") }));
    }

    [Fact]
    public async Task Failed_batch_leaves_no_files_behind()
    {
        // First file saves (original + thumbnail), then the disk "fills up" on the second file.
        _storage.FailNextSave = false;
        var files = new[] { File("a.jpg"), File("b.jpg") };
        var storage = _storage;
        var failing = new FailingAfter(storage, saves: 2);
        var service = new SurveyImageService(new FakeSurveyRepository(_db), new FakeSurveyImageRepository(_db), _db,
            failing, _processor, _clock);

        await Assert.ThrowsAsync<IOException>(() => service.UploadAsync(_survey.Id, files));
        Assert.Empty(storage.Files);
        Assert.Empty(_db.Images);
    }

    [Fact]
    public async Task List_open_and_delete()
    {
        await Upload(File("a.jpg"));
        var image = Assert.Single(await _service.ListAsync(_survey.Id));

        var thumb = await _service.OpenAsync(image.Id, thumbnail: true);
        await using (thumb.Content) Assert.Equal("image/jpeg", thumb.ContentType);
        var original = await _service.OpenAsync(image.Id, thumbnail: false);
        await using (original.Content) Assert.Equal("a.jpg", original.FileName);

        await _service.DeleteAsync(image.Id);
        Assert.Empty(_db.Images);
        Assert.Empty(_storage.Files);
        Assert.Equal(SurveyStatus.Draft, _survey.Status); // last photo removed

        await Assert.ThrowsAsync<NotFoundException>(() => _service.OpenAsync(image.Id, thumbnail: false));
    }

    [Fact]
    public async Task Photos_cannot_be_deleted_once_review_starts()
    {
        await Upload(File("a.jpg"));
        _survey.Status = SurveyStatus.InReview;
        await Assert.ThrowsAsync<RequestValidationException>(() => _service.DeleteAsync(_db.Images[0].Id));
    }

    /// <summary>Lets the first N saves through, then throws like a full disk.</summary>
    private sealed class FailingAfter : IFileStorage
    {
        private readonly FakeFileStorage _inner;
        private int _remaining;
        public FailingAfter(FakeFileStorage inner, int saves) { _inner = inner; _remaining = saves; }

        public Task<long> SaveAsync(string key, Stream content, CancellationToken ct = default) =>
            _remaining-- > 0 ? _inner.SaveAsync(key, content, ct) : throw new IOException("Disk full");
        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default) => _inner.OpenReadAsync(key, ct);
        public Task DeleteAsync(string key, CancellationToken ct = default) => _inner.DeleteAsync(key, ct);
        public Task DeleteFolderAsync(string folderKey, CancellationToken ct = default) => _inner.DeleteFolderAsync(folderKey, ct);
    }
}
