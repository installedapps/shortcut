namespace Shortcut.Api.Analyses;

public sealed record EditSetting(string Group, string Name, string Value, string Rationale);

public sealed record AnalysisResponse(
    Guid Id,
    string FileName,
    DateTimeOffset CreatedAt,
    string Summary,
    IReadOnlyList<EditSetting> LightroomSettings,
    IReadOnlyList<EditSetting> DarktableSettings);
