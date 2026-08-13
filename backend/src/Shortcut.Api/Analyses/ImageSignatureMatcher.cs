namespace Shortcut.Api.Analyses;

public static class ImageSignatureMatcher
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] TiffLittleEndianSignature = [0x49, 0x49, 0x2A, 0x00];
    private static readonly byte[] TiffBigEndianSignature = [0x4D, 0x4D, 0x00, 0x2A];

    public static async Task<bool> MatchesAsync(
        IFormFile photo,
        Stream stream,
        CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        stream.Position = 0;

        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => StartsWith(header, bytesRead, JpegSignature),
            ".png" => StartsWith(header, bytesRead, PngSignature),
            ".webp" => IsWebp(header, bytesRead),
            ".tif" or ".tiff" => StartsWith(header, bytesRead, TiffLittleEndianSignature) ||
                StartsWith(header, bytesRead, TiffBigEndianSignature),
            _ => false
        };
    }

    private static bool StartsWith(byte[] header, int bytesRead, byte[] signature) =>
        bytesRead >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature);

    private static bool IsWebp(byte[] header, int bytesRead) =>
        bytesRead >= 12 &&
        header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
        header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
}
