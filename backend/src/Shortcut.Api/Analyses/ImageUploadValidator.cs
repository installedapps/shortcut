namespace Shortcut.Api.Analyses;

public static class ImageUploadValidator
{
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

    public static bool IsSupported(IFormFile photo)
    {
        var extension = Path.GetExtension(photo.FileName);
        return SupportedContentTypes.Contains(photo.ContentType)
            && SupportedExtensions.Contains(extension);
    }
}
