using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Wfp.SurveyImages.Models;
using AerialWatch.Contracts.Wfp.SurveyImages.Services;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>
/// Survey photos (D-030): upload in batches of up to 20, list, view (original or thumbnail), delete.
/// Routes: upload/{surveyId}, list/{surveyId}, original/{id}, thumbnail/{id}, delete/{id} under /api/wfp/surveyImages.
/// Files are served only through these authenticated endpoints, never as static files.
/// </summary>
public sealed class SurveyImagesController : AppControllerBase
{
    // 20 photos × 50 MB plus multipart overhead. Raised for this action only; the rest of the API keeps Kestrel's 30 MB.
    private const long MaxUploadRequestBytes = 20L * 50 * 1024 * 1024 + 10L * 1024 * 1024;

    private readonly ISurveyImageService _images;

    public SurveyImagesController(ISurveyImageService images) => _images = images;

    /// <summary>POST /api/wfp/surveyImages/upload/{surveyId} as multipart/form-data with one or more "files" parts.</summary>
    [HttpPost("{surveyId:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
    [ProducesResponseType<UploadImagesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadImagesResponse>> Upload(Guid surveyId, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        // ASP.NET buffers each part to memory/disk, so these streams are seekable (the service reads them more than once).
        var streams = new List<Stream>(files.Count);
        try
        {
            var uploaded = files.Select(f =>
            {
                var stream = f.OpenReadStream();
                streams.Add(stream);
                return new UploadedFile(f.FileName, f.Length, stream);
            }).ToList();
            return Ok(await _images.UploadAsync(surveyId, uploaded, ct));
        }
        finally
        {
            foreach (var s in streams) await s.DisposeAsync();
        }
    }

    [HttpGet("{surveyId:guid}")]
    [ProducesResponseType<IReadOnlyList<SurveyImageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SurveyImageDto>>> List(Guid surveyId, CancellationToken ct) =>
        Ok(await _images.ListAsync(surveyId, ct));

    /// <summary>The original photo (for the review tool and download).</summary>
    [HttpGet("{id:guid}")]
    [Produces("image/jpeg", "image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Original(Guid id, CancellationToken ct) => Send(id, thumbnail: false, ct);

    /// <summary>A ~400 px JPEG preview (falls back to the original when there is none).</summary>
    [HttpGet("{id:guid}")]
    [Produces("image/jpeg", "image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Thumbnail(Guid id, CancellationToken ct) => Send(id, thumbnail: true, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _images.DeleteAsync(id, ct);
        return NoContent();
    }

    private async Task<IActionResult> Send(Guid id, bool thumbnail, CancellationToken ct)
    {
        var file = await _images.OpenAsync(id, thumbnail, ct);
        // A photo id always refers to the same bytes, so the browser may cache it (privately: data is not public).
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(file.Content, file.ContentType, enableRangeProcessing: true); // FileStreamResult disposes the stream
    }
}
