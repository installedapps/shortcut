namespace Shortcut.Api.Analyses;

public static class DarktableSettingsFilter
{
    public static readonly string[] AllowedModules =
    [
        "AgX",
        "local contrast",
        "color balance RGB",
        "color equalizer",
        "tone equalizer"
    ];

    public static IReadOnlyList<EditSetting> KeepAllowed(IReadOnlyList<EditSetting> settings) =>
        settings
            .Where(setting =>
                AllowedModules.Contains(setting.Group, StringComparer.OrdinalIgnoreCase) &&
                !ContainsBlockedDisplayTransform(setting))
            .ToArray();

    private static bool ContainsBlockedDisplayTransform(EditSetting setting)
    {
        var text = $"{setting.Group} {setting.Name} {setting.Value} {setting.Rationale}";
        return text.Contains("filmic", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("sigmoid", StringComparison.OrdinalIgnoreCase);
    }
}
