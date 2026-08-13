using Microsoft.AspNetCore.Http;
using Shortcut.Api.Analyses;
using Xunit;

namespace Shortcut.Api.Tests;

public sealed class PhotoUploadValidatorTests
{
    private readonly PhotoUploadValidator _validator = new();

    [Fact]
    public async Task ValidateAsyncRejectsNonImageUploads()
    {
        var upload = CreateUpload("notes.txt", "text/plain", "not a photo"u8.ToArray());

        var result = await _validator.ValidateAsync(upload, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("unsupported_upload", result.ErrorType);
    }

    [Fact]
    public async Task ValidateAsyncRejectsImageMetadataWithInvalidSignature()
    {
        var upload = CreateUpload("portrait.jpg", "image/jpeg", "not a jpeg"u8.ToArray());

        var result = await _validator.ValidateAsync(upload, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("unsupported_upload", result.ErrorType);
    }

    [Fact]
    public async Task ValidateAsyncRejectsMalformedPngUploads()
    {
        var upload = CreateUpload("broken.png", "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var result = await _validator.ValidateAsync(upload, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("unsupported_upload", result.ErrorType);
    }

    [Fact]
    public async Task ValidateAsyncRejectsBlankSingleColorPngUploads()
    {
        var upload = CreateUpload("blank.png", "image/png", PngTestImageFactory.CreateSolidColor(width: 3, height: 2));

        var result = await _validator.ValidateAsync(upload, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("blank_image_upload", result.ErrorType);
        Assert.Contains("blank single-color image", result.Message);
    }

    [Fact]
    public async Task ValidateAsyncAcceptsPngUploadsWithVisiblePixelVariation()
    {
        var upload = CreateUpload("photo.png", "image/png", PngTestImageFactory.CreateWithPixelVariation(width: 3, height: 2));

        var result = await _validator.ValidateAsync(upload, CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsyncAcceptsJpegUploadsWithMatchingSignature()
    {
        var upload = CreateUpload("photo.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10]);

        var result = await _validator.ValidateAsync(upload, CancellationToken.None);

        Assert.True(result.IsValid);
    }

    private static FormFile CreateUpload(string fileName, string contentType, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "photo", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

}
