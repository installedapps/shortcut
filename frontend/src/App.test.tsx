import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import App from "./App";

const renderApp = () => {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });

  return render(
    <QueryClientProvider client={client}>
      <App />
    </QueryClientProvider>
  );
};

describe("App", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () =>
        Response.json({
          id: "analysis-1",
          fileName: "portrait.jpg",
          createdAt: "2026-08-13T10:00:00Z",
          summary: "Warm editorial portrait with soft contrast.",
          lightroomSettings: [
            { group: "Basic", name: "Temperature", value: "6200 K", rationale: "Adds amber warmth." },
            { group: "Basic", name: "Tint", value: "+6", rationale: "Keeps skin from leaning green." },
            { group: "Basic", name: "Vibrance", value: "+14", rationale: "Adds controlled color." },
            { group: "Basic", name: "Saturation", value: "-3", rationale: "Avoids oversaturation." },
            { group: "Color Grading", name: "Shadows", value: "Hue 220 / Sat 8 / Lum -2", rationale: "Cools darker tones." },
            { group: "Color Grading", name: "Midtones", value: "Hue 34 / Sat 10 / Lum +3", rationale: "Warms skin and surfaces." },
            { group: "Color Grading", name: "Highlights", value: "Hue 48 / Sat 6 / Lum +2", rationale: "Adds golden highlight bias." }
          ],
          darktableSettings: [
            { group: "AgX", name: "look", value: "medium high contrast", rationale: "Use one scene-referred display transform." },
            { group: "local contrast", name: "detail", value: "+12%", rationale: "Adds mid-scale structure." },
            { group: "color balance RGB", name: "global chroma", value: "+8%", rationale: "Warms the grade." },
            { group: "color equalizer", name: "orange saturation", value: "+10%", rationale: "Shapes warm hues." },
            { group: "tone equalizer", name: "shadows", value: "+0.3 EV", rationale: "Opens shadow detail." }
          ]
        })
      )
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.mocked(navigator.clipboard.writeText).mockClear();
  });

  it("uploads a photograph and renders starting edit settings", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await waitFor(() => expect(fetch).toHaveBeenCalledWith("/api/analyses", expect.any(Object)));
    expect(await screen.findByText(/warm editorial portrait/i)).toBeInTheDocument();
    expect(screen.getAllByText("Temperature")).toHaveLength(2);
    expect(screen.getAllByText("6200 K")).toHaveLength(2);
    expect(screen.getAllByText("Tint")).toHaveLength(2);
    expect(screen.getAllByText("+6")).toHaveLength(2);
    expect(screen.getByText("Vibrance")).toBeInTheDocument();
    expect(screen.getByText("+14")).toBeInTheDocument();
    expect(screen.getByText("Saturation")).toBeInTheDocument();
    expect(screen.getByText("-3")).toBeInTheDocument();
    expect(screen.getAllByText("Color Grading")).toHaveLength(3);
    expect(screen.getAllByText("Shadows")).toHaveLength(2);
    expect(screen.getAllByText("Midtones")).toHaveLength(2);
    expect(screen.getAllByText("Highlights")).toHaveLength(2);
  });

  it("renders accurate Lightroom temperature, tint, and HSL color controls from generated values", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findAllByText("Temperature");

    const temperatureMeter = screen.getByRole("meter", { name: "Temperature color position" });
    expect(temperatureMeter).toHaveAttribute("aria-valuenow", "6200");
    expect(temperatureMeter).toHaveAttribute("aria-valuemin", "2000");
    expect(temperatureMeter).toHaveAttribute("aria-valuemax", "50000");
    expect(temperatureMeter).toHaveStyle({ "--marker-position": "8.75%" });

    const tintMeter = screen.getByRole("meter", { name: "Tint color position" });
    expect(tintMeter).toHaveAttribute("aria-valuenow", "6");
    expect(tintMeter).toHaveAttribute("aria-valuemin", "-150");
    expect(tintMeter).toHaveAttribute("aria-valuemax", "150");
    expect(tintMeter).toHaveStyle({ "--marker-position": "52%" });

    expect(screen.getByLabelText("Shadows HSL wheel")).toHaveStyle({
      "--marker-x": "47.43%",
      "--marker-y": "47.84%",
      "--marker-hue": "220deg",
      "--marker-saturation": "8%",
      "--marker-luminance": "-2%"
    });
    expect(screen.getByLabelText("Midtones HSL wheel")).toHaveStyle({
      "--marker-x": "53.48%",
      "--marker-y": "52.35%",
      "--marker-hue": "34deg",
      "--marker-saturation": "10%",
      "--marker-luminance": "3%"
    });
    expect(screen.getByLabelText("Highlights HSL wheel")).toHaveStyle({
      "--marker-x": "51.69%",
      "--marker-y": "51.87%",
      "--marker-hue": "48deg",
      "--marker-saturation": "6%",
      "--marker-luminance": "2%"
    });
  });

  it("renders Lightroom color controls when the API uses alternate color grading wording", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      Response.json({
        id: "analysis-1",
        fileName: "portrait.jpg",
        createdAt: "2026-08-13T10:00:00Z",
        summary: "Warm editorial portrait with soft contrast.",
        lightroomSettings: [
          { group: "Basic", name: "Temperature", value: "6200 K", rationale: "Adds amber warmth." },
          { group: "Basic", name: "Tint", value: "+6", rationale: "Keeps skin from leaning green." },
          { group: "Basic", name: "Vibrance", value: "+14", rationale: "Adds controlled color." },
          { group: "Basic", name: "Saturation", value: "-3", rationale: "Avoids oversaturation." },
          {
            group: "Color Grading",
            name: "Shadows",
            value: "Hue: 220 deg, Saturation: 8%, Luminance: -2",
            rationale: "Cools darker tones."
          },
          { group: "Color Grading", name: "Midtones", value: "H 34 / S 10 / L +3", rationale: "Warms skin and surfaces." },
          { group: "Color Grading", name: "Highlights", value: "48 / 6 / +2", rationale: "Adds golden highlight bias." }
        ],
        darktableSettings: []
      })
    );
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    expect(await screen.findByRole("meter", { name: "Temperature color position" })).toHaveStyle({ "--marker-position": "8.75%" });
    expect(screen.getByRole("meter", { name: "Tint color position" })).toHaveStyle({ "--marker-position": "52%" });
    expect(screen.getByLabelText("Shadows HSL wheel")).toHaveStyle({
      "--marker-x": "47.43%",
      "--marker-y": "47.84%",
      "--marker-hue": "220deg",
      "--marker-saturation": "8%",
      "--marker-luminance": "-2%"
    });
    expect(screen.getByLabelText("Midtones HSL wheel")).toHaveStyle({
      "--marker-x": "53.48%",
      "--marker-y": "52.35%",
      "--marker-hue": "34deg",
      "--marker-saturation": "10%",
      "--marker-luminance": "3%"
    });
    expect(screen.getByLabelText("Highlights HSL wheel")).toHaveStyle({
      "--marker-x": "51.69%",
      "--marker-y": "51.87%",
      "--marker-hue": "48deg",
      "--marker-saturation": "6%",
      "--marker-luminance": "2%"
    });
  });

  it("keeps Lightroom temperature and tint sliders visible when a color grading value cannot be parsed", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      Response.json({
        id: "analysis-1",
        fileName: "portrait.jpg",
        createdAt: "2026-08-13T10:00:00Z",
        summary: "Warm editorial portrait with soft contrast.",
        lightroomSettings: [
          { group: "Basic", name: "Temperature", value: "6200 K", rationale: "Adds amber warmth." },
          { group: "Basic", name: "Tint", value: "+6", rationale: "Keeps skin from leaning green." },
          { group: "Basic", name: "Vibrance", value: "+14", rationale: "Adds controlled color." },
          { group: "Basic", name: "Saturation", value: "-3", rationale: "Avoids oversaturation." },
          { group: "Color Grading", name: "Shadows", value: "Cool shadows", rationale: "Cools darker tones." },
          { group: "Color Grading", name: "Midtones", value: "Warm midtones", rationale: "Warms skin and surfaces." },
          { group: "Color Grading", name: "Highlights", value: "Golden highlights", rationale: "Adds golden highlight bias." }
        ],
        darktableSettings: []
      })
    );
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    expect(await screen.findByRole("meter", { name: "Temperature color position" })).toBeInTheDocument();
    expect(screen.getByRole("meter", { name: "Tint color position" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Shadows HSL wheel")).not.toBeInTheDocument();
  });

  it("uses a hamburger menu to switch from Lightroom to Darktable settings", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findAllByText("Temperature");
    await userEvent.click(screen.getByRole("button", { name: /open settings menu/i }));
    await userEvent.click(screen.getByRole("menuitem", { name: /darktable settings/i }));

    expect(screen.getByText("AgX")).toBeInTheDocument();
    expect(screen.getByText("local contrast")).toBeInTheDocument();
    expect(screen.getByText("color balance RGB")).toBeInTheDocument();
    expect(screen.getByText("color equalizer")).toBeInTheDocument();
    expect(screen.getByText("tone equalizer")).toBeInTheDocument();
    expect(screen.queryByText("Temperature")).not.toBeInTheDocument();
    expect(screen.queryByRole("meter", { name: "Temperature color position" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Shadows HSL wheel")).not.toBeInTheDocument();
  });

  it("reacts when switching settings formats back and forth", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findAllByText("Temperature");
    await userEvent.click(screen.getByRole("button", { name: /open settings menu/i }));
    await userEvent.click(screen.getByRole("menuitem", { name: /darktable settings/i }));
    expect(screen.getByText("AgX")).toBeInTheDocument();
    expect(screen.queryByText("Temperature")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /open settings menu/i }));
    await userEvent.click(screen.getByRole("menuitem", { name: /lightroom settings/i }));
    expect(screen.getAllByText("Temperature")).toHaveLength(2);
    expect(screen.queryByText("AgX")).not.toBeInTheDocument();
  });

  it("renders the AgX-only Darktable module set without alternate display transforms", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findAllByText("Temperature");
    await userEvent.click(screen.getByRole("button", { name: /open settings menu/i }));
    await userEvent.click(screen.getByRole("menuitem", { name: /darktable settings/i }));

    expect(screen.getByText("Use AgX as the display transform, then make only the listed module tweaks.")).toBeInTheDocument();
    expect(screen.getByText("AgX")).toBeInTheDocument();
    expect(screen.getByText("local contrast")).toBeInTheDocument();
    expect(screen.getByText("color balance RGB")).toBeInTheDocument();
    expect(screen.getByText("color equalizer")).toBeInTheDocument();
    expect(screen.getByText("tone equalizer")).toBeInTheDocument();
    expect(screen.queryByText(/filmic/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/sigmoid/i)).not.toBeInTheDocument();
  });

  it("shows a loading spinner while settings are generating", async () => {
    let resolveFetch: (response: Response) => void = () => {};
    vi.mocked(fetch).mockImplementationOnce(
      () =>
        new Promise<Response>((resolve) => {
          resolveFetch = resolve;
        })
    );
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    expect(screen.getByRole("status", { name: /generating settings/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /generating settings/i })).toBeDisabled();

    resolveFetch(
      Response.json({
        id: "analysis-1",
        fileName: "portrait.jpg",
        createdAt: "2026-08-13T10:00:00Z",
        summary: "Warm editorial portrait with soft contrast.",
        lightroomSettings: [
          { group: "Basic", name: "Temperature", value: "6200 K", rationale: "Adds amber warmth." },
          { group: "Basic", name: "Tint", value: "+6", rationale: "Keeps skin from leaning green." },
          { group: "Basic", name: "Vibrance", value: "+14", rationale: "Adds controlled color." },
          { group: "Basic", name: "Saturation", value: "-3", rationale: "Avoids oversaturation." },
          { group: "Color Grading", name: "Shadows", value: "Hue 220 / Sat 8 / Lum -2", rationale: "Cools darker tones." },
          { group: "Color Grading", name: "Midtones", value: "Hue 34 / Sat 10 / Lum +3", rationale: "Warms skin and surfaces." },
          { group: "Color Grading", name: "Highlights", value: "Hue 48 / Sat 6 / Lum +2", rationale: "Adds golden highlight bias." }
        ],
        darktableSettings: []
      })
    );
    expect(await screen.findByText(/warm editorial portrait/i)).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByRole("status", { name: /generating settings/i })).not.toBeInTheDocument());
  });

  it("copies an individual setting value", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findAllByText("Temperature");
    await userEvent.click(screen.getAllByRole("button", { name: "Copy" })[0]);

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith("Basic / Temperature: 6200 K");
    expect(screen.getByRole("button", { name: "Copied" })).toBeInTheDocument();
  });

  it("shows a friendly error when the API returns malformed JSON", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response("{not json", {
        status: 200,
        headers: { "Content-Type": "application/json" }
      })
    );
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    expect(await screen.findByText("The analysis response was malformed. Try generating settings again.")).toBeInTheDocument();
  });

  it("shows a friendly error when the API returns incorrect setting values", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      Response.json({
        id: "analysis-1",
        fileName: "portrait.jpg",
        createdAt: "2026-08-13T10:00:00Z",
        summary: "Warm editorial portrait with soft contrast.",
        lightroomSettings: [
          { group: "Basic", name: "Temperature", value: "+11", rationale: "Adds amber warmth." }
        ],
        darktableSettings: []
      })
    );
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    expect(await screen.findByText("The analysis response included invalid Lightroom values. Try generating settings again.")).toBeInTheDocument();
  });

  it("reacts to drag-and-drop photo selection", async () => {
    renderApp();

    const dropZone = screen.getByText("Choose a reference image").closest("label");
    const file = new File(["fake image"], "dropped.png", { type: "image/png" });
    expect(dropZone).not.toBeNull();

    fireEvent.dragEnter(dropZone!, { dataTransfer: { files: [file] } });
    expect(dropZone).toHaveClass("drop-zone-active");

    fireEvent.drop(dropZone!, { dataTransfer: { files: [file] } });
    expect(dropZone).not.toHaveClass("drop-zone-active");
    expect(screen.getByText("dropped.png / <0.1 MB")).toBeInTheDocument();
    expect(screen.getByAltText("Preview of dropped.png")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /generate settings/i })).toBeEnabled();
  });

  it("clears generated results and copied state when a different photo is selected", async () => {
    renderApp();

    const firstFile = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), firstFile);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findAllByText("Temperature");
    await userEvent.click(screen.getAllByRole("button", { name: "Copy" })[0]);
    expect(screen.getByRole("button", { name: "Copied" })).toBeInTheDocument();

    const secondFile = new File(["next image"], "second.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), secondFile);

    expect(screen.queryByText(/warm editorial portrait/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Copied" })).not.toBeInTheDocument();
    expect(screen.queryByRole("meter", { name: "Temperature color position" })).not.toBeInTheDocument();
    expect(screen.getByText("second.jpg / <0.1 MB")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /generate settings/i })).toBeEnabled();
  });

  it("regenerates Lightroom color controls for a different photo upload", async () => {
    vi.mocked(fetch).mockReset();
    vi.mocked(fetch)
      .mockResolvedValueOnce(
        Response.json({
          id: "analysis-1",
          fileName: "portrait.jpg",
          createdAt: "2026-08-13T10:00:00Z",
          summary: "Warm editorial portrait with soft contrast.",
          lightroomSettings: [
            { group: "Basic", name: "Temperature", value: "6200 K", rationale: "Adds amber warmth." },
            { group: "Basic", name: "Tint", value: "+6", rationale: "Keeps skin from leaning green." },
            { group: "Basic", name: "Vibrance", value: "+14", rationale: "Adds controlled color." },
            { group: "Basic", name: "Saturation", value: "-3", rationale: "Avoids oversaturation." },
            { group: "Color Grading", name: "Shadows", value: "Hue 220 / Sat 8 / Lum -2", rationale: "Cools darker tones." },
            { group: "Color Grading", name: "Midtones", value: "Hue 34 / Sat 10 / Lum +3", rationale: "Warms skin and surfaces." },
            { group: "Color Grading", name: "Highlights", value: "Hue 48 / Sat 6 / Lum +2", rationale: "Adds golden highlight bias." }
          ],
          darktableSettings: []
        })
      )
      .mockResolvedValueOnce(Response.json({
        id: "analysis-2",
        fileName: "cool.jpg",
        createdAt: "2026-08-13T10:10:00Z",
        summary: "Cool editorial frame with cyan shadows.",
        lightroomSettings: [
          { group: "Basic", name: "Temperature", value: "4800 K", rationale: "Cools the white balance." },
          { group: "Basic", name: "Tint", value: "-12", rationale: "Adds a slight green pull." },
          { group: "Basic", name: "Vibrance", value: "+8", rationale: "Keeps color present." },
          { group: "Basic", name: "Saturation", value: "-5", rationale: "Controls strong colors." },
          { group: "Color Grading", name: "Shadows", value: "Hue 198 / Sat 12 / Lum -4", rationale: "Pushes shadows cyan." },
          { group: "Color Grading", name: "Midtones", value: "Hue 24 / Sat 8 / Lum +1", rationale: "Keeps skin warm." },
          { group: "Color Grading", name: "Highlights", value: "Hue 56 / Sat 5 / Lum +3", rationale: "Warms highlights." }
        ],
        darktableSettings: []
      }));
    renderApp();

    const firstFile = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), firstFile);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));
    await screen.findAllByText("Temperature");
    expect(screen.getByRole("meter", { name: "Temperature color position" })).toHaveStyle({ "--marker-position": "8.75%" });

    const secondFile = new File(["next image"], "cool.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), secondFile);
    expect(screen.queryByRole("meter", { name: "Temperature color position" })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    const regeneratedTemperatureMeter = await screen.findByRole("meter", { name: "Temperature color position" });
    expect(regeneratedTemperatureMeter).toHaveAttribute("aria-valuenow", "4800");
    expect(regeneratedTemperatureMeter).toHaveStyle({ "--marker-position": "5.83%" });
    expect(screen.getByRole("meter", { name: "Tint color position" })).toHaveStyle({ "--marker-position": "46%" });
    expect(screen.getByLabelText("Shadows HSL wheel")).toHaveStyle({
      "--marker-x": "45.21%",
      "--marker-y": "48.44%",
      "--marker-hue": "198deg",
      "--marker-saturation": "12%",
      "--marker-luminance": "-4%"
    });
  });

  it("requires an image before analysis can be requested", () => {
    renderApp();

    expect(screen.getByRole("button", { name: /generate settings/i })).toBeDisabled();
  });
});
