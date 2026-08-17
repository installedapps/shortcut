namespace Shortcut.Api.Analyses;

public sealed class KimiAnalysisRequestFactory(string model)
{
    public object Create(string imageUrl) => new
    {
        model,
        messages = new object[]
        {
            new
            {
                role = "system",
                content = """
                    You are a professional photo editor. Analyze the reference photo and return concise practical starting settings for Lightroom and Darktable.
                    Output only a JSON object with this shape:
                    {
                      "summary": "one sentence summary of the intended grade",
                      "lightroomSettings": [
                        { "group": "Basic", "name": "Exposure", "value": "+0.25", "rationale": "short reason" }
                      ],
                      "darktableSettings": [
                        { "group": "AgX", "name": "look", "value": "medium high contrast", "rationale": "short reason" }
                      ]
                    }
                    Include 8 to 10 Lightroom settings and 5 to 8 Darktable settings.
                    Lightroom Temperature value must be an absolute Kelvin value such as "6200 K", never a relative value such as "+11".
                    Lightroom Tint, Vibrance, and Saturation values must include an explicit + or - sign such as "+6" or "-3".
                    Lightroom must include Color Grading settings for Shadows, Midtones, and Highlights.
                    Darktable settings must use only these modules as the group value: AgX, local contrast, color balance RGB, color equalizer, tone equalizer.
                    For Darktable, use AgX as the only display transform. Never mention or recommend any other display transform or any module outside the allowed list.
                    Make every Darktable setting a tweakable control within its module, not a general instruction.
                    """
            },
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = imageUrl
                        }
                    },
                    new
                    {
                        type = "text",
                        text = "Generate editing settings for matching this photo's color, tone, contrast, and texture."
                    }
                }
            }
        },
        response_format = new
        {
            type = "json_object"
        },
        thinking = new
        {
            type = "disabled"
        },
        max_completion_tokens = 1600
    };
}
