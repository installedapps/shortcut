using System.Buffers.Binary;

namespace Shortcut.Api.Analyses;

internal sealed record PngPayload(int Width, int Height, byte ColorType, byte[] ImageData)
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static async Task<PngPayload?> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var signature = new byte[Signature.Length];
        if (await stream.ReadAsync(signature, cancellationToken) != signature.Length ||
            !signature.SequenceEqual(Signature))
        {
            return null;
        }

        var idat = new MemoryStream();
        var (width, height, colorType) = (0, 0, (byte)0);
        while (TryReadChunk(stream, out var type, out var data))
        {
            if (type == "IHDR" && data.Length >= 13)
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
                height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(4, 4));
                colorType = data[9];
            }
            else if (type == "IDAT")
            {
                idat.Write(data);
            }
            else if (type == "IEND")
            {
                break;
            }
        }

        return width > 0 && height > 0 && idat.Length > 0
            ? new PngPayload(width, height, colorType, idat.ToArray())
            : null;
    }

    private static bool TryReadChunk(Stream stream, out string type, out byte[] data)
    {
        type = "";
        data = [];
        Span<byte> lengthBytes = stackalloc byte[4];
        Span<byte> typeBytes = stackalloc byte[4];
        if (stream.Read(lengthBytes) != 4 || stream.Read(typeBytes) != 4)
        {
            return false;
        }

        var length = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);
        if (length < 0)
        {
            return false;
        }

        data = new byte[length];
        type = System.Text.Encoding.ASCII.GetString(typeBytes);
        return stream.Read(data) == length && stream.Seek(4, SeekOrigin.Current) >= 0;
    }
}
