namespace Shortcut.Api.Analyses;

public sealed class PhotoUploadValidator : IPhotoUploadValidator
{
    public const long MaxPhotoUploadBytes = 20 * 1024 * 1024;

    private const string UnsupportedMessage = "Only JPG, PNG, WebP, or TIFF image uploads are supported.";

    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/tiff"
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".tif",
        ".tiff"
    };

    public async Task<PhotoUploadValidationResult> ValidateAsync(
        IFormFile? photo,
        CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0)
        {
            return PhotoUploadValidationResult.Failure("missing_upload", "Upload a photograph to analyze.");
        }

        if (photo.Length > MaxPhotoUploadBytes)
        {
            return PhotoUploadValidationResult.Failure(
                "oversized_upload",
                "Upload an image smaller than 20 MB. Large source photos take too long to analyze directly.");
        }

        if (!HasSupportedMetadata(photo))
        {
            return PhotoUploadValidationResult.Failure("unsupported_upload", UnsupportedMessage);
        }

        await using var stream = photo.OpenReadStream();
        if (!await ImageSignatureMatcher.MatchesAsync(photo, stream, cancellationToken))
        {
            return PhotoUploadValidationResult.Failure("unsupported_upload", UnsupportedMessage);
        }

        if (Path.GetExtension(photo.FileName).Equals(".png", StringComparison.OrdinalIgnoreCase) &&
            !await PngBlankImageDetector.IsReadableAsync(stream, cancellationToken))
        {
            return PhotoUploadValidationResult.Failure("unsupported_upload", UnsupportedMessage);
        }

        if (await PngBlankImageDetector.IsSolidColorAsync(stream, cancellationToken))
        {
            return PhotoUploadValidationResult.Failure(
                "blank_image_upload",
                "Upload a photograph with visible detail instead of a blank single-color image.");
        }

        return PhotoUploadValidationResult.Success;
    }

    private static bool HasSupportedMetadata(IFormFile photo)
    {
        var extension = Path.GetExtension(photo.FileName);
        return SupportedContentTypes.Contains(photo.ContentType)
            && SupportedExtensions.Contains(extension);
    }
}
