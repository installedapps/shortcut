using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shortcut.Api.Analyses;

public sealed class KimiPhotoAnalysisService(
    HttpClient httpClient,
    IConfiguration configuration,
    KimiAnalysisRequestFactory requestFactory) : IPhotoAnalysisService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _apiKey = new[]
        {
            configuration["Kimi:ApiKey"],
            configuration["Moonshot:ApiKey"],
            configuration["MOONSHOT_API_KEY"]
        }.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? throw new InvalidOperationException("Configure Kimi:ApiKey before using Kimi photo analysis.");

    public async Task<AnalysisResponse> AnalyzeAsync(
        string fileName,
        string contentType,
        Stream photo,
        CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await photo.CopyToAsync(memory, cancellationToken);
        var imageUrl = $"data:{NormalizeContentType(contentType)};base64,{Convert.ToBase64String(memory.ToArray())}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(requestFactory.Create(imageUrl), options: SerializerOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Kimi analysis failed: {(int)response.StatusCode} {response.ReasonPhrase}. {responseBody}");
        }

        var completion = JsonSerializer.Deserialize<KimiChatCompletion>(responseBody, SerializerOptions)
            ?? throw new InvalidOperationException("Kimi returned an empty response.");
        var content = completion.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Kimi returned no analysis content.");
        }

        var generated = JsonSerializer.Deserialize<KimiGeneratedAnalysis>(content, SerializerOptions)
            ?? throw new InvalidOperationException("Kimi returned analysis content that could not be parsed.");

        return new AnalysisResponse(
            Guid.NewGuid(),
            fileName,
            DateTimeOffset.UtcNow,
            generated.Summary,
            generated.LightroomSettings,
            DarktableSettingsFilter.KeepAllowed(generated.DarktableSettings));
    }

    private static string NormalizeContentType(string contentType) =>
        contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg" : contentType;

    private sealed record KimiChatCompletion(IReadOnlyList<KimiChoice> Choices);

    private sealed record KimiChoice(KimiMessage Message);

    private sealed record KimiMessage(string Content);

    private sealed record KimiGeneratedAnalysis(
        string Summary,
        IReadOnlyList<EditSetting> LightroomSettings,
        IReadOnlyList<EditSetting> DarktableSettings);
}
