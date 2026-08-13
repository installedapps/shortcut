using System.Buffers.Binary;
using System.IO.Compression;

namespace Shortcut.Api.Tests;

public static class PngTestImageFactory
{
    public static byte[] CreateSolidColor(int width, int height) =>
        Create(width, height, makeLastPixelDifferent: false);

    public static byte[] CreateWithPixelVariation(int width, int height) =>
        Create(width, height, makeLastPixelDifferent: true);

    private static byte[] Create(int width, int height, bool makeLastPixelDifferent)
    {
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(png, "IHDR", CreateIhdr(width, height));
        WriteChunk(png, "IDAT", Compress(CreateRgbScanlines(width, height, makeLastPixelDifferent)));
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static byte[] CreateIhdr(int width, int height)
    {
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4, 4), height);
        ihdr[8] = 8;
        ihdr[9] = 2;
        return ihdr;
    }

    private static byte[] CreateRgbScanlines(int width, int height, bool makeLastPixelDifferent)
    {
        var scanlineLength = 1 + width * 3;
        var raw = new byte[scanlineLength * height];
        for (var row = 0; row < height; row++)
        {
            var rowOffset = row * scanlineLength;
            for (var pixel = rowOffset + 1; pixel < rowOffset + scanlineLength; pixel += 3)
            {
                raw[pixel] = 120;
                raw[pixel + 1] = 80;
                raw[pixel + 2] = 40;
            }
        }

        if (makeLastPixelDifferent)
        {
            raw[^1] = 41;
        }

        return raw;
    }

    private static byte[] Compress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            zlib.Write(bytes);
        }

        return output.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(System.Text.Encoding.ASCII.GetBytes(type));
        stream.Write(data);
        stream.Write([0, 0, 0, 0]);
    }
}
