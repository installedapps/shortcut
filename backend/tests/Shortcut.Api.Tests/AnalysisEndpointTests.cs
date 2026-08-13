using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shortcut.Api.Analyses;
using Xunit;

namespace Shortcut.Api.Tests;

public sealed class AnalysisEndpointTests
{
    [Fact]
    public async Task PostAnalysesAcceptsPhotographAndReturnsLightroomAndDarktableSettings()
    {
        await using var app = CreateAppWithAnalysisService<HeuristicPhotoAnalysisService>(
            Directory.CreateTempSubdirectory("shortcut-errors-").FullName);

        using var client = app.CreateClient();
        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(CreateJpegBytes());
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(image, "photo", "portrait.jpg");

        var response = await client.PostAsync("/api/analyses", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisResponse>();
        Assert.NotNull(body);
        Assert.Equal("portrait.jpg", body.FileName);
        Assert.Contains(body.LightroomSettings, setting => setting.Name == "Temperature");
        Assert.Contains(body.LightroomSettings, setting => setting.Group == "Tone Curve");
        Assert.Contains(body.DarktableSettings, setting => setting.Group == "AgX");
        Assert.Contains(body.DarktableSettings, setting => setting.Group == "local contrast");
        Assert.Contains(body.DarktableSettings, setting => setting.Group == "color balance RGB");
        Assert.Contains(body.DarktableSettings, setting => setting.Group == "color equalizer");
        Assert.Contains(body.DarktableSettings, setting => setting.Group == "tone equalizer");
        var allowedDarktableModules = new[]
        {
            "AgX",
            "local contrast",
            "color balance RGB",
            "color equalizer",
            "tone equalizer"
        };
        Assert.All(body.DarktableSettings, setting => Assert.Contains(setting.Group, allowedDarktableModules));
        Assert.DoesNotContain(body.DarktableSettings, setting =>
            $"{setting.Group} {setting.Name} {setting.Value} {setting.Rationale}".Contains("filmic", StringComparison.OrdinalIgnoreCase) ||
            $"{setting.Group} {setting.Name} {setting.Value} {setting.Rationale}".Contains("sigmoid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PostAnalysesRejectsNonImageUploads()
    {
        await using var app = CreateAppWithAnalysisService<HeuristicPhotoAnalysisService>(
            Directory.CreateTempSubdirectory("shortcut-errors-").FullName);
        using var client = app.CreateClient();
        using var content = new MultipartFormDataContent();
        var document = new StringContent("not a photo");
        document.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(document, "photo", "notes.txt");

        var response = await client.PostAsync("/api/analyses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAnalysesRejectsBlankSingleColorImagesBeforeCallingAnalysisService()
    {
        await using var app = CreateAppWithAnalysisService<ThrowingPhotoAnalysisService>(
            Directory.CreateTempSubdirectory("shortcut-errors-").FullName);
        using var client = app.CreateClient();
        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(PngTestImageFactory.CreateSolidColor(width: 3, height: 2));
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "photo", "blank.png");

        var response = await client.PostAsync("/api/analyses", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("blank single-color image", body);
    }

    [Fact]
    public async Task PostAnalysesRejectsLargeImageUploadsBeforeCallingAnalysisService()
    {
        await using var app = CreateAppWithAnalysisService<ThrowingPhotoAnalysisService>(
            Directory.CreateTempSubdirectory("shortcut-errors-").FullName);
        using var client = app.CreateClient();
        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(new byte[20 * 1024 * 1024 + 1]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(image, "photo", "large.jpg");

        var response = await client.PostAsync("/api/analyses", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("smaller than 20 MB", body);
    }

    [Fact]
    public async Task PostAnalysesReturnsGatewayTimeoutWhenAnalysisTimesOut()
    {
        var logDirectory = Directory.CreateTempSubdirectory("shortcut-errors-").FullName;
        await using var app = CreateAppWithAnalysisService<TimeoutPhotoAnalysisService>(logDirectory);
        using var client = app.CreateClient();
        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(CreateJpegBytes());
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(image, "photo", "portrait.jpg");

        var response = await client.PostAsync("/api/analyses", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.Contains("timed out", body);

        var logFile = Assert.Single(Directory.GetFiles(logDirectory, "*.log"));
        var log = await File.ReadAllTextAsync(logFile);
        Assert.Contains("error_type: kimi_timeout", log);
        Assert.Contains("message: Kimi analysis timed out before settings were returned.", log);
        Assert.Contains("file_name: portrait.jpg", log);
    }

    private static WebApplicationFactory<Program> CreateAppWithAnalysisService<TAnalysisService>(
        string? logDirectory = null)
        where TAnalysisService : class, IPhotoAnalysisService =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                if (logDirectory is not null)
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                    {
                        configuration.AddInMemoryCollection(
                        [
                            new KeyValuePair<string, string?>("LogFiles:ErrorDirectory", logDirectory)
                        ]);
                    });
                }

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IPhotoAnalysisService>();
                    services.AddSingleton<IPhotoAnalysisService, TAnalysisService>();
                    services.RemoveAll<IAnalysisRepository>();
                    services.AddSingleton<IAnalysisRepository, InMemoryAnalysisRepository>();
                });
            });

    private static byte[] CreateJpegBytes() => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    private sealed class TimeoutPhotoAnalysisService : IPhotoAnalysisService
    {
        public Task<AnalysisResponse> AnalyzeAsync(
            string fileName,
            string contentType,
            Stream photo,
            CancellationToken cancellationToken) =>
            throw new TaskCanceledException("The request timed out.");
    }

    private sealed class ThrowingPhotoAnalysisService : IPhotoAnalysisService
    {
        public Task<AnalysisResponse> AnalyzeAsync(
            string fileName,
            string contentType,
            Stream photo,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("This service should not be called.");
    }
}
