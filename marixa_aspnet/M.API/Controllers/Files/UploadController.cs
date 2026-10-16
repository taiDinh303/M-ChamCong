using M.API.Storage;
using M.Contract.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static M.Core.Base.BaseException;

namespace M.API.Controllers.Files;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class UploadController(IPhotoStorage storage, ILogger<UploadController> logger) : BaseApiController
{
    [HttpPost("photo")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(IFormFile? file, string type = "checkin",
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0 || file.Length > PhotoStorageBase.MaxBytes
            || !PhotoStorageBase.IsAllowedExtension(Path.GetExtension(file.FileName))
            || type is not ("checkin" or "checkout"))
            throw new ErrorException(400, "INVALID_PHOTO", "Choose a jpg/png/webp photo up to 5 MiB and a valid attendance type.");
        try
        {
            await using var stream = file.OpenReadStream();
            var url = await storage.SaveAsync(stream, Path.GetExtension(file.FileName), type, cancellationToken);
            return Success(url);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (ArgumentException)
        {
            throw new ErrorException(400, "INVALID_PHOTO", "Photo content is invalid or exceeds 5 MiB.");
        }
        catch (Exception exception) when (exception is IOException or HttpRequestException or InvalidOperationException)
        {
            logger.LogError("Photo upload failed ({ExceptionType}); trace {TraceId}", exception.GetType().Name,
                HttpContext.TraceIdentifier);
            throw new ErrorException(502, "UPLOAD_FAILED", "Unable to store the photo. Please try again.");
        }
    }
}
