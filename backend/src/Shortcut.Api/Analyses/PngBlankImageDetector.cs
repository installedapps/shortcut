using System.IO.Compression;

namespace Shortcut.Api.Analyses;

public static class PngBlankImageDetector
{
    public static async Task<bool> IsReadableAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var png = await PngPayload.ReadAsync(stream, cancellationToken);
        stream.Position = 0;
        return png is not null;
    }

    public static async Task<bool> IsSolidColorAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            stream.Position = 0;
            var png = await PngPayload.ReadAsync(stream, cancellationToken);
            stream.Position = 0;

            if (png is null || png.Width <= 1 || png.Height <= 1 || !TryGetBytesPerPixel(png.ColorType, out var bytesPerPixel))
            {
                return false;
            }

            await using var compressed = new MemoryStream(png.ImageData);
            await using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
            using var raw = new MemoryStream();
            await zlib.CopyToAsync(raw, cancellationToken);

            var pixels = PngScanlineDecoder.Decode(raw.ToArray(), png.Width, png.Height, bytesPerPixel);
            return pixels is not null && IsSolidColor(pixels, bytesPerPixel);
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool IsSolidColor(byte[] pixels, int bytesPerPixel)
    {
        if (pixels.Length <= bytesPerPixel)
        {
            return false;
        }

        var firstPixel = pixels.AsSpan(0, bytesPerPixel);
        for (var pixel = bytesPerPixel; pixel < pixels.Length; pixel += bytesPerPixel)
        {
            if (!pixels.AsSpan(pixel, bytesPerPixel).SequenceEqual(firstPixel))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetBytesPerPixel(byte colorType, out int bytesPerPixel)
    {
        bytesPerPixel = colorType switch
        {
            0 => 1,
            2 => 3,
            6 => 4,
            _ => 0
        };
        return bytesPerPixel > 0;
    }
}
