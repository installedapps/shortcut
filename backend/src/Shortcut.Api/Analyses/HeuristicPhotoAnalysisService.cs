namespace Shortcut.Api.Analyses;

public sealed class HeuristicPhotoAnalysisService : IPhotoAnalysisService
{
    public async Task<AnalysisResponse> AnalyzeAsync(
        string fileName,
        string contentType,
        Stream photo,
        CancellationToken cancellationToken)
    {
        var sample = new byte[Math.Min(photo.Length > 0 ? (int)Math.Min(photo.Length, 4096) : 4096, 4096)];
        var bytesRead = await photo.ReadAsync(sample, cancellationToken);
        var brightnessHint = bytesRead == 0 ? 128 : sample.Take(bytesRead).Average(value => value);
        var isLikelyBright = brightnessHint > 136;

        var lightroomSettings = new List<EditSetting>
        {
            new("Basic", "Temperature", "+8", "Adds a restrained orange warmth without overwhelming neutral greys."),
            new("Basic", "Tint", "+3", "Keeps skin and highlights from leaning too green."),
            new("Basic", "Exposure", isLikelyBright ? "-0.20" : "+0.25", "Moves the file toward a balanced midtone baseline."),
            new("Basic", "Contrast", "+12", "Creates a cleaner starting look while preserving edit room."),
            new("Basic", "Highlights", isLikelyBright ? "-28" : "-14", "Recovers brighter regions before local adjustments."),
            new("Basic", "Shadows", "+18", "Opens darker detail for a softer photographic grade."),
            new("Presence", "Texture", "-5", "Slightly smooths fine digital bite."),
            new("Presence", "Clarity", "+6", "Adds enough structure to keep the image from becoming flat."),
            new("Color Mixer", "Orange Saturation", "+10", "Strengthens warm subject tones for the requested look."),
            new("Color Mixer", "Blue Saturation", "-12", "Reduces cool distractions and supports the grey-orange palette."),
            new("Tone Curve", "Highlights", "-10", "Softens the shoulder of the curve for a calmer highlight rolloff."),
            new("Tone Curve", "Shadows", "+8", "Lifts the toe slightly for a muted matte base.")
        };

        var darktableSettings = new List<EditSetting>
        {
            new("AgX", "look", "medium high contrast", "Use AgX as the single display transform for a natural tone map."),
            new("AgX", "white relative exposure", isLikelyBright ? "+3.0 EV" : "+3.6 EV", "Set the upper exposure range before refining with the auto tune levels picker."),
            new("AgX", "black relative exposure", "-7.0 EV", "Anchors the shadow range while keeping a soft toe."),
            new("AgX", "contrast", "1.10", "Adds a restrained curve slope around the pivot."),
            new("AgX", "saturation", "1.06", "Gives the rendered image a small warm color lift after tone mapping."),
            new("local contrast", "detail", "+12%", "Adds mid-scale structure without reaching for extra sharpening modules."),
            new("color balance RGB", "global chroma", "+8%", "Uses scene-referred grading for broad color intensity."),
            new("color balance RGB", "highlights warmth", "+4%", "Adds a subtle warm highlight bias while keeping neutrals controlled."),
            new("color equalizer", "orange saturation", "+10%", "Strengthens warm subject tones selectively."),
            new("color equalizer", "blue saturation", "-12%", "Reduces cool distractions and supports the grey-orange palette."),
            new("tone equalizer", "shadows", "+0.3 EV", "Opens darker detail while preserving the overall contrast shape."),
            new("tone equalizer", "highlights", "-0.4 EV", "Controls brighter regions without changing the display transform.")
        };

        return new AnalysisResponse(
            Guid.NewGuid(),
            fileName,
            DateTimeOffset.UtcNow,
            "Warm grey-orange grade with soft contrast, controlled highlights, and lifted shadow detail.",
            lightroomSettings,
            darktableSettings);
    }
}
