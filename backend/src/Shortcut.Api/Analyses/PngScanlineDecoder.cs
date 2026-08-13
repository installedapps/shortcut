namespace Shortcut.Api.Analyses;

internal static class PngScanlineDecoder
{
    public static byte[]? Decode(byte[] raw, int width, int height, int bytesPerPixel)
    {
        var rowLength = width * bytesPerPixel;
        var scanlineLength = rowLength + 1;
        if (raw.Length < scanlineLength * height)
        {
            return null;
        }

        var pixels = new byte[rowLength * height];
        for (var row = 0; row < height; row++)
        {
            var rawOffset = row * scanlineLength;
            var pixelOffset = row * rowLength;
            var priorOffset = pixelOffset - rowLength;
            if (!DecodeRow(raw, pixels, rawOffset, pixelOffset, priorOffset, rowLength, bytesPerPixel))
            {
                return null;
            }
        }

        return pixels;
    }

    private static bool DecodeRow(
        byte[] raw,
        byte[] pixels,
        int rawOffset,
        int pixelOffset,
        int priorOffset,
        int rowLength,
        int bytesPerPixel)
    {
        var filter = raw[rawOffset];
        for (var index = 0; index < rowLength; index++)
        {
            var value = raw[rawOffset + 1 + index];
            pixels[pixelOffset + index] = filter switch
            {
                0 => value,
                1 => Add(value, Left(pixels, pixelOffset, index, bytesPerPixel)),
                2 => Add(value, Up(pixels, priorOffset, index)),
                3 => Add(value, Average(Left(pixels, pixelOffset, index, bytesPerPixel), Up(pixels, priorOffset, index))),
                4 => Add(value, Paeth(Left(pixels, pixelOffset, index, bytesPerPixel), Up(pixels, priorOffset, index), UpperLeft(pixels, priorOffset, index, bytesPerPixel))),
                _ => 0
            };

            if (filter > 4)
            {
                return false;
            }
        }

        return true;
    }

    private static byte Left(byte[] pixels, int rowOffset, int index, int bytesPerPixel) =>
        index >= bytesPerPixel ? pixels[rowOffset + index - bytesPerPixel] : (byte)0;

    private static byte Up(byte[] pixels, int priorOffset, int index) =>
        priorOffset >= 0 ? pixels[priorOffset + index] : (byte)0;

    private static byte UpperLeft(byte[] pixels, int priorOffset, int index, int bytesPerPixel) =>
        priorOffset >= 0 && index >= bytesPerPixel ? pixels[priorOffset + index - bytesPerPixel] : (byte)0;

    private static byte Average(byte left, byte up) => (byte)((left + up) / 2);

    private static byte Add(byte value, byte predictor) => unchecked((byte)(value + predictor));

    private static byte Paeth(byte left, byte up, byte upperLeft)
    {
        var estimate = left + up - upperLeft;
        var leftDistance = Math.Abs(estimate - left);
        var upDistance = Math.Abs(estimate - up);
        var upperLeftDistance = Math.Abs(estimate - upperLeft);
        if (leftDistance <= upDistance && leftDistance <= upperLeftDistance)
        {
            return left;
        }

        return upDistance <= upperLeftDistance ? up : upperLeft;
    }
}
