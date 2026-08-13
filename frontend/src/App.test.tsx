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
            { group: "Basic", name: "Temperature", value: "+11", rationale: "Adds amber warmth." },
            { group: "Tone Curve", name: "Highlights", value: "-18", rationale: "Recovers bright skin tones." }
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
    expect(screen.getByText("Temperature")).toBeInTheDocument();
    expect(screen.getByText("+11")).toBeInTheDocument();
    expect(screen.getByText("Tone Curve")).toBeInTheDocument();
  });

  it("uses a hamburger menu to switch from Lightroom to Darktable settings", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findByText("Temperature");
    await userEvent.click(screen.getByRole("button", { name: /open settings menu/i }));
    await userEvent.click(screen.getByRole("menuitem", { name: /darktable settings/i }));

    expect(screen.getByText("AgX")).toBeInTheDocument();
    expect(screen.getByText("local contrast")).toBeInTheDocument();
    expect(screen.getByText("color balance RGB")).toBeInTheDocument();
    expect(screen.getByText("color equalizer")).toBeInTheDocument();
    expect(screen.getByText("tone equalizer")).toBeInTheDocument();
    expect(screen.queryByText("Temperature")).not.toBeInTheDocument();
  });

  it("reacts when switching settings formats back and forth", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findByText("Temperature");
    await userEvent.click(screen.getByRole("button", { name: /open settings menu/i }));
    await userEvent.click(screen.getByRole("menuitem", { name: /darktable settings/i }));
    expect(screen.getByText("AgX")).toBeInTheDocument();
    expect(screen.queryByText("Temperature")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /open settings menu/i }));
    await userEvent.click(screen.getByRole("menuitem", { name: /lightroom settings/i }));
    expect(screen.getByText("Temperature")).toBeInTheDocument();
    expect(screen.queryByText("AgX")).not.toBeInTheDocument();
  });

  it("renders the AgX-only Darktable module set without alternate display transforms", async () => {
    renderApp();

    const file = new File(["fake image"], "portrait.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), file);
    await userEvent.click(screen.getByRole("button", { name: /generate settings/i }));

    await screen.findByText("Temperature");
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
          { group: "Basic", name: "Temperature", value: "+11", rationale: "Adds amber warmth." }
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

    await screen.findByText("Temperature");
    await userEvent.click(screen.getAllByRole("button", { name: "Copy" })[0]);

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith("Basic / Temperature: +11");
    expect(screen.getByRole("button", { name: "Copied" })).toBeInTheDocument();
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

    await screen.findByText("Temperature");
    await userEvent.click(screen.getAllByRole("button", { name: "Copy" })[0]);
    expect(screen.getByRole("button", { name: "Copied" })).toBeInTheDocument();

    const secondFile = new File(["next image"], "second.jpg", { type: "image/jpeg" });
    await userEvent.upload(screen.getByLabelText(/photograph/i), secondFile);

    expect(screen.queryByText(/warm editorial portrait/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Copied" })).not.toBeInTheDocument();
    expect(screen.getByText("second.jpg / <0.1 MB")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /generate settings/i })).toBeEnabled();
  });

  it("requires an image before analysis can be requested", () => {
    renderApp();

    expect(screen.getByRole("button", { name: /generate settings/i })).toBeDisabled();
  });
});
