import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
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
            { group: "Display Transform", name: "AgX", value: "Use blender-like base preset", rationale: "Use one scene-referred display transform." },
            { group: "Color", name: "color balance rgb", value: "Global chroma +8%", rationale: "Warms the grade." },
            { group: "Color", name: "color equalizer", value: "Orange saturation +10%", rationale: "Shapes warm hues." },
            { group: "Tone", name: "tone equalizer", value: "Shadows +0.3 EV", rationale: "Opens shadow detail." }
          ]
        })
      )
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
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
    expect(screen.getByText("color balance rgb")).toBeInTheDocument();
    expect(screen.getByText("color equalizer")).toBeInTheDocument();
    expect(screen.getByText("tone equalizer")).toBeInTheDocument();
    expect(screen.queryByText("Temperature")).not.toBeInTheDocument();
  });

  it("requires an image before analysis can be requested", () => {
    renderApp();

    expect(screen.getByRole("button", { name: /generate settings/i })).toBeDisabled();
  });
});
