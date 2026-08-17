using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Shortcut.Api.Analyses;

public sealed class KimiPhotoAnalysisService(
    HttpClient httpClient,
    IConfiguration configuration,
    KimiAnalysisRequestFactory requestFactory) : IPhotoAnalysisService
{
    private static readonly Regex KelvinValue = new(@"^\d{4,5}\s?K$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SignedValue = new(@"^[+-]\d+(?:\.\d+)?%?$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _apiKey = KimiConfiguration.ReadApiKey(configuration)
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

        KimiChatCompletion? completion;
        try
        {
            completion = JsonSerializer.Deserialize<KimiChatCompletion>(responseBody, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Kimi returned malformed API JSON.", exception);
        }

        if (completion is null)
        {
            throw new InvalidOperationException("Kimi returned an empty response.");
        }

        var content = completion.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Kimi returned no analysis content.");
        }

        KimiGeneratedAnalysis? generated;
        try
        {
            generated = JsonSerializer.Deserialize<KimiGeneratedAnalysis>(content, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Kimi returned malformed analysis JSON.", exception);
        }

        if (generated is null)
        {
            throw new InvalidOperationException("Kimi returned analysis content that could not be parsed.");
        }

        ValidateGeneratedAnalysis(generated);

        return new AnalysisResponse(
            Guid.NewGuid(),
            fileName,
            DateTimeOffset.UtcNow,
            generated.Summary,
            generated.LightroomSettings ?? [],
            DarktableSettingsFilter.KeepAllowed(generated.DarktableSettings ?? []));
    }

    private static string NormalizeContentType(string contentType) =>
        contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg" : contentType;

    private static void ValidateGeneratedAnalysis(KimiGeneratedAnalysis generated)
    {
        var errors = new List<string>();
        var settings = generated.LightroomSettings ?? [];

        ValidateSettingValue(settings, "Temperature", KelvinValue, "Temperature must be an absolute Kelvin value", errors);
        ValidateSettingValue(settings, "Tint", SignedValue, "Tint must include an explicit + or - sign", errors);
        ValidateSettingValue(settings, "Vibrance", SignedValue, "Vibrance must include an explicit + or - sign", errors);
        ValidateSettingValue(settings, "Saturation", SignedValue, "Saturation must include an explicit + or - sign", errors);

        foreach (var colorGradingName in new[] { "Shadows", "Midtones", "Highlights" })
        {
            if (!settings.Any(setting =>
                IsNamed(setting.Group, "Color Grading") &&
                IsNamed(setting.Name, colorGradingName)))
            {
                errors.Add($"Color Grading must include {colorGradingName}");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Kimi returned invalid Lightroom settings: {string.Join("; ", errors)}.");
        }
    }

    private static void ValidateSettingValue(
        IReadOnlyList<EditSetting> settings,
        string name,
        Regex pattern,
        string message,
        List<string> errors)
    {
        var setting = settings.FirstOrDefault(setting => IsNamed(setting.Name, name));
        if (setting is null || !pattern.IsMatch(setting.Value.Trim()))
        {
            errors.Add(message);
        }
    }

    private static bool IsNamed(string value, string expected) =>
        value.Equals(expected, StringComparison.OrdinalIgnoreCase);

    private sealed record KimiChatCompletion(IReadOnlyList<KimiChoice> Choices);

    private sealed record KimiChoice(KimiMessage Message);

    private sealed record KimiMessage(string Content);

    private sealed record KimiGeneratedAnalysis(
        string Summary,
        IReadOnlyList<EditSetting> LightroomSettings,
        IReadOnlyList<EditSetting> DarktableSettings);
}
