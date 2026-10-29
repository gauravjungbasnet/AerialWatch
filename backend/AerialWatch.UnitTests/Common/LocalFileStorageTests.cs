using System.Text;
using Xunit;
using AerialWatch.Infrastructure.Storage;

namespace AerialWatch.UnitTests.Common;

/// <summary>Disk storage (D-030) in a throw-away temp folder: round trip, no overwrite, and keys can't escape the root.</summary>
public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aw-storage-test-" + Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests() => _storage = new LocalFileStorage(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static MemoryStream Bytes(string text) => new(Encoding.UTF8.GetBytes(text));

    /// <summary>Opens and closes the file again: an undisposed FileStream locks the file on Windows.</summary>
    private async Task<bool> ExistsAsync(string key)
    {
        await using var stream = await _storage.OpenReadAsync(key);
        return stream is not null;
    }

    [Fact]
    public async Task Save_open_and_delete_round_trip()
    {
        var written = await _storage.SaveAsync("wfp/surveys/s1/originals/a.jpg", Bytes("hello"));
        Assert.Equal(5, written);

        await using (var stream = await _storage.OpenReadAsync("wfp/surveys/s1/originals/a.jpg"))
        {
            Assert.NotNull(stream);
            using var reader = new StreamReader(stream!);
            Assert.Equal("hello", await reader.ReadToEndAsync());
        }

        Assert.Empty(Directory.GetFiles(_root, "*.tmp", SearchOption.AllDirectories)); // temp file renamed away

        await _storage.DeleteAsync("wfp/surveys/s1/originals/a.jpg");
        Assert.False(await ExistsAsync("wfp/surveys/s1/originals/a.jpg"));
        await _storage.DeleteAsync("wfp/surveys/s1/originals/a.jpg"); // already gone: no error
    }

    [Fact]
    public async Task Never_overwrites_an_existing_file()
    {
        await _storage.SaveAsync("x/a.jpg", Bytes("first"));
        await Assert.ThrowsAsync<IOException>(() => _storage.SaveAsync("x/a.jpg", Bytes("second")));
    }

    [Fact]
    public async Task Delete_folder_removes_everything_under_it()
    {
        await _storage.SaveAsync("wfp/surveys/s1/originals/a.jpg", Bytes("a"));
        await _storage.SaveAsync("wfp/surveys/s1/thumbs/a.jpg", Bytes("a"));
        await _storage.SaveAsync("wfp/surveys/s2/originals/b.jpg", Bytes("b"));

        await _storage.DeleteFolderAsync("wfp/surveys/s1");

        Assert.False(await ExistsAsync("wfp/surveys/s1/originals/a.jpg"));
        Assert.True(await ExistsAsync("wfp/surveys/s2/originals/b.jpg"));
        await _storage.DeleteFolderAsync("wfp/surveys/missing"); // no error
    }

    [Theory]
    [InlineData("../outside.jpg")]
    [InlineData("wfp/../../outside.jpg")]
    [InlineData("wfp/./a.jpg")]
    [InlineData("/etc/passwd")]
    [InlineData(@"C:\Windows\win.ini")]
    [InlineData(@"wfp\a.jpg")]
    [InlineData("wfp//a.jpg")]
    [InlineData("wfp/a b.jpg")]
    [InlineData("")]
    public async Task Refuses_keys_that_could_escape_the_root(string key)
    {
        Assert.Throws<ArgumentException>(() => _storage.Resolve(key));
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.SaveAsync(key, Bytes("x")));
    }
}
