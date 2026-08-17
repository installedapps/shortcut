namespace Shortcut.Api.Analyses;

public static class KimiConfiguration
{
    private static readonly string[] ApiKeyKeys =
    [
        "Kimi:ApiKey",
        "Moonshot:ApiKey",
        "KIMI_API_KEY",
        "MOONSHOT_API_KEY"
    ];

    public static string? ReadApiKey(IConfiguration configuration) =>
        ApiKeyKeys
            .Select(configuration.GetValue<string?>)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();
}
