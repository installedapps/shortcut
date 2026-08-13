using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Shortcut.Api.Analyses;
using Xunit;

namespace Shortcut.Api.Tests;

public sealed class KimiPhotoAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsyncFiltersDarktableSettingsToAllowedModules()
    {
        var handler = new CapturingHandler(_ => new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(new
                        {
                            summary = "Warm controlled edit.",
                            lightroomSettings = new[]
                            {
                                new { group = "Basic", name = "Exposure", value = "+0.25", rationale = "Balances midtones." }
                            },
                            darktableSettings = new[]
                            {
                                new { group = "AgX", name = "look", value = "medium high contrast", rationale = "Single display transform." },
                                new { group = "local contrast", name = "detail", value = "+12%", rationale = "Adds structure." },
                                new { group = "color balance RGB", name = "global chroma", value = "+8%", rationale = "Adds color." },
                                new { group = "color equalizer", name = "orange saturation", value = "+10%", rationale = "Warms hues." },
                                new { group = "tone equalizer", name = "shadows", value = "+0.3 EV", rationale = "Opens shadows." },
                                new { group = "Display Transform", name = "AgX", value = "base", rationale = "Old grouping should be removed." },
                                new { group = "AgX", name = "look", value = "sigmoid-like", rationale = "Blocked wording should be removed." },
                                new { group = "filmic rgb", name = "contrast", value = "1.2", rationale = "Blocked module should be removed." }
                            }
                        })
                    }
                }
            }
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new KimiPhotoAnalysisService(
            httpClient,
            CreateConfiguration(),
            new KimiAnalysisRequestFactory("test-model"));

        var analysis = await service.AnalyzeAsync(
            "portrait.jpg",
            "image/jpeg",
            new MemoryStream([1, 2, 3, 4]),
            CancellationToken.None);

        var allowedModules = new[]
        {
            "AgX",
            "local contrast",
            "color balance RGB",
            "color equalizer",
            "tone equalizer"
        };
        Assert.Equal(5, analysis.DarktableSettings.Count);
        Assert.All(analysis.DarktableSettings, setting => Assert.Contains(setting.Group, allowedModules));
        Assert.DoesNotContain(analysis.DarktableSettings, setting =>
            $"{setting.Group} {setting.Name} {setting.Value} {setting.Rationale}".Contains("filmic", StringComparison.OrdinalIgnoreCase) ||
            $"{setting.Group} {setting.Name} {setting.Value} {setting.Rationale}".Contains("sigmoid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsyncRequestsConciseAllowedDarktableModules()
    {
        var handler = new CapturingHandler(_ => new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(new
                        {
                            summary = "Warm controlled edit.",
                            lightroomSettings = new[]
                            {
                                new { group = "Basic", name = "Exposure", value = "+0.25", rationale = "Balances midtones." }
                            },
                            darktableSettings = new[]
                            {
                                new { group = "AgX", name = "look", value = "medium high contrast", rationale = "Single display transform." }
                            }
                        })
                    }
                }
            }
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new KimiPhotoAnalysisService(
            httpClient,
            CreateConfiguration(),
            new KimiAnalysisRequestFactory("test-model"));

        await service.AnalyzeAsync(
            "portrait.jpg",
            "image/jpg",
            new MemoryStream([1, 2, 3, 4]),
            CancellationToken.None);

        Assert.NotNull(handler.RequestJson);
        using var document = JsonDocument.Parse(handler.RequestJson);
        var root = document.RootElement;
        var systemPrompt = root.GetProperty("messages")[0].GetProperty("content").GetString();

        Assert.Equal("test-model", root.GetProperty("model").GetString());
        Assert.Equal(1600, root.GetProperty("max_completion_tokens").GetInt32());
        Assert.Equal("disabled", root.GetProperty("thinking").GetProperty("type").GetString());
        Assert.Contains("AgX, local contrast, color balance RGB, color equalizer, tone equalizer", systemPrompt);
        Assert.DoesNotContain("filmic", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sigmoid", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data:image/jpeg;base64,", handler.RequestJson);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Kimi:ApiKey", "test-api-key"),
                new KeyValuePair<string, string?>("Kimi:Model", "test-model")
            ])
            .Build();

    private sealed class CapturingHandler(Func<HttpRequestMessage, object> responseFactory) : HttpMessageHandler
    {
        public string? RequestJson { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestJson = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responseFactory(request))
            };
        }
    }
}
