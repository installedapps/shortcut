using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shortcut.Api.Analyses;
using Xunit;

namespace Shortcut.Api.Tests;

public sealed class AnalysisEndpointTests
{
    [Fact]
    public async Task PostAnalysesAcceptsPhotographAndReturnsLightroomAndDarktableSettings()
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IAnalysisRepository, InMemoryAnalysisRepository>();
                });
            });

        using var client = app.CreateClient();
        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent([1, 2, 3, 4]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(image, "photo", "portrait.jpg");

        var response = await client.PostAsync("/api/analyses", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisResponse>();
        Assert.NotNull(body);
        Assert.Equal("portrait.jpg", body.FileName);
        Assert.Contains(body.LightroomSettings, setting => setting.Name == "Temperature");
        Assert.Contains(body.LightroomSettings, setting => setting.Group == "Tone Curve");
        Assert.Contains(body.DarktableSettings, setting => setting.Name == "AgX");
        Assert.Contains(body.DarktableSettings, setting => setting.Name == "color balance rgb");
        Assert.Contains(body.DarktableSettings, setting => setting.Name == "color equalizer");
        Assert.Contains(body.DarktableSettings, setting => setting.Name == "tone equalizer");
    }

    [Fact]
    public async Task PostAnalysesRejectsNonImageUploads()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();
        using var content = new MultipartFormDataContent();
        var document = new StringContent("not a photo");
        document.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(document, "photo", "notes.txt");

        var response = await client.PostAsync("/api/analyses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
