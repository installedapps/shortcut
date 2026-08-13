import { useMutation } from "@tanstack/react-query";
import { ChangeEvent, useEffect, useMemo, useState } from "react";
import { AnalysisResponse, createAnalysis } from "./api";

type SettingsMode = "lightroom" | "darktable";

function App() {
  const [photo, setPhoto] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [settingsMode, setSettingsMode] = useState<SettingsMode>("lightroom");
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const analysis = useMutation<AnalysisResponse, Error, File>({
    mutationFn: createAnalysis
  });

  useEffect(() => {
    if (!photo) {
      setPreviewUrl(null);
      return;
    }

    const nextPreviewUrl = URL.createObjectURL(photo);
    setPreviewUrl(nextPreviewUrl);

    return () => URL.revokeObjectURL(nextPreviewUrl);
  }, [photo]);

  const selectedPhotoMeta = useMemo(() => {
    if (!photo) {
      return "JPG, PNG, WebP, or TIFF";
    }

    const sizeInMb = photo.size / 1024 / 1024;
    return `${photo.name} / ${sizeInMb < 0.1 ? "<0.1" : sizeInMb.toFixed(1)} MB`;
  }, [photo]);

  const onPhotoChange = (event: ChangeEvent<HTMLInputElement>) => {
    const nextPhoto = event.target.files?.[0] ?? null;
    setPhoto(nextPhoto);
    analysis.reset();
  };

  const activeSettings = analysis.data
    ? settingsMode === "lightroom"
      ? analysis.data.lightroomSettings
      : analysis.data.darktableSettings
    : [];

  const activeLabel = settingsMode === "lightroom" ? "Lightroom settings" : "Darktable settings";

  return (
    <main className="app-shell">
      <section className="workspace" aria-labelledby="app-title">
        <div className="intro">
          <p className="eyebrow">Shortcut</p>
          <h1 id="app-title">Translate a reference photo into edit settings.</h1>
          <p>
            Upload a photograph and get a practical Lightroom or Darktable starting point for color,
            tone, curve, and presence adjustments.
          </p>
        </div>

        <div className="tool-grid">
          <form
            className="upload-panel"
            onSubmit={(event) => {
              event.preventDefault();
              if (photo) {
                analysis.mutate(photo);
              }
            }}
          >
            <label className="drop-zone">
              <span className="drop-zone-label">Photograph</span>
              <span className="drop-zone-text">Choose a reference image</span>
              <span className="drop-zone-meta">{selectedPhotoMeta}</span>
              <input type="file" accept="image/*" aria-label="Photograph" onChange={onPhotoChange} />
            </label>

            {previewUrl ? (
              <img className="photo-preview" src={previewUrl} alt={`Preview of ${photo?.name}`} />
            ) : (
              <div className="empty-preview" aria-hidden="true">
                <span>Preview</span>
              </div>
            )}

            <button className="primary-button" type="submit" disabled={!photo || analysis.isPending}>
              {analysis.isPending ? "Generating..." : "Generate settings"}
            </button>
            {analysis.isError ? <p className="error-message">{analysis.error.message}</p> : null}
          </form>

          <section className="results-panel" aria-live="polite" aria-label="Generated edit settings">
            {analysis.data ? (
              <>
                <div className="result-header">
                  <div>
                    <p className="eyebrow">{activeLabel}</p>
                    <h2>{analysis.data.fileName}</h2>
                  </div>
                  <div className="result-actions">
                    <time dateTime={analysis.data.createdAt}>
                      {new Intl.DateTimeFormat(undefined, {
                        month: "short",
                        day: "numeric",
                        hour: "numeric",
                        minute: "2-digit"
                      }).format(new Date(analysis.data.createdAt))}
                    </time>
                    <div className="menu-wrap">
                      <button
                        className="menu-button"
                        type="button"
                        aria-label="Open settings menu"
                        aria-expanded={isMenuOpen}
                        aria-haspopup="menu"
                        onClick={() => setIsMenuOpen((value) => !value)}
                      >
                        <span />
                        <span />
                        <span />
                      </button>
                      {isMenuOpen ? (
                        <div className="settings-menu" role="menu" aria-label="Settings format">
                          <button
                            type="button"
                            role="menuitem"
                            aria-current={settingsMode === "lightroom"}
                            onClick={() => {
                              setSettingsMode("lightroom");
                              setIsMenuOpen(false);
                            }}
                          >
                            Lightroom settings
                          </button>
                          <button
                            type="button"
                            role="menuitem"
                            aria-current={settingsMode === "darktable"}
                            onClick={() => {
                              setSettingsMode("darktable");
                              setIsMenuOpen(false);
                            }}
                          >
                            Darktable settings
                          </button>
                        </div>
                      ) : null}
                    </div>
                  </div>
                </div>
                <p className="summary">{analysis.data.summary}</p>
                {settingsMode === "darktable" ? (
                  <p className="module-note">
                    Use one display transform: AgX is listed as the recommended module here. If you
                    prefer sigmoid or filmic rgb, use it instead of AgX rather than combining them.
                  </p>
                ) : null}
                <div className="settings-list">
                  {activeSettings.map((setting) => (
                    <article className="setting-card" key={`${setting.group}-${setting.name}`}>
                      <div>
                        <span className="setting-group">{setting.group}</span>
                        <h3>{setting.name}</h3>
                        <p>{setting.rationale}</p>
                      </div>
                      <strong>{setting.value}</strong>
                    </article>
                  ))}
                </div>
              </>
            ) : (
              <div className="results-empty">
                <p className="eyebrow">Awaiting image</p>
                <h2>No settings generated yet.</h2>
                <p>Results will appear here as grouped adjustments ready to try in your editor.</p>
              </div>
            )}
          </section>
        </div>
      </section>
    </main>
  );
}

export default App;
