import { useMutation } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import type { CSSProperties, ChangeEvent, DragEvent } from "react";
import { AnalysisResponse, EditSetting, createAnalysis } from "./api";

type SettingsMode = "lightroom" | "darktable";
type ColorWheelValue = {
  name: "Shadows" | "Midtones" | "Highlights";
  hue: number;
  saturation: number;
  luminance: number;
  markerX: string;
  markerY: string;
};

const TEMPERATURE_MIN = 2000;
const TEMPERATURE_MAX = 50000;
const TINT_MIN = -150;
const TINT_MAX = 150;

function App() {
  const [photo, setPhoto] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [settingsMode, setSettingsMode] = useState<SettingsMode>("lightroom");
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isDraggingPhoto, setIsDraggingPhoto] = useState(false);
  const [copiedSettingKey, setCopiedSettingKey] = useState<string | null>(null);

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
    setCopiedSettingKey(null);
  };

  const onDropPhoto = (event: DragEvent<HTMLLabelElement>) => {
    event.preventDefault();
    setIsDraggingPhoto(false);
    const nextPhoto = event.dataTransfer.files?.[0] ?? null;
    if (nextPhoto) {
      setPhoto(nextPhoto);
      analysis.reset();
      setCopiedSettingKey(null);
    }
  };

  const copySetting = async (setting: string, key: string) => {
    await navigator.clipboard?.writeText(setting);
    setCopiedSettingKey(key);
    window.setTimeout(() => setCopiedSettingKey(null), 1800);
  };

  const activeSettings = analysis.data
    ? settingsMode === "lightroom"
      ? analysis.data.lightroomSettings
      : analysis.data.darktableSettings
    : [];
  const lightroomColorControls = useMemo(
    () => (analysis.data ? getLightroomColorControls(analysis.data.lightroomSettings) : null),
    [analysis.data]
  );

  const activeLabel = settingsMode === "lightroom" ? "Lightroom settings" : "Darktable settings";

  return (
    <main className="app-shell">
      <section className="workspace" aria-labelledby="app-title">
        <div className="intro">
          <p className="eyebrow">Shortcut</p>
          <h1 id="app-title">Translate a reference photo into edit settings.</h1>
          <p>
            Upload a photograph and get a practical Lightroom or Darktable starting point for color,
            tone, curve, and other adjustments.
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
            <label
              className={`drop-zone${isDraggingPhoto ? " drop-zone-active" : ""}`}
              onDragEnter={(event) => {
                event.preventDefault();
                setIsDraggingPhoto(true);
              }}
              onDragOver={(event) => event.preventDefault()}
              onDragLeave={() => setIsDraggingPhoto(false)}
              onDrop={onDropPhoto}
            >
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
              {analysis.isPending ? (
                <>
                  <span className="loading-spinner" aria-hidden="true" />
                  Generating settings
                </>
              ) : (
                "Generate settings"
              )}
            </button>
            {analysis.isError ? <p className="error-message">{analysis.error.message}</p> : null}
          </form>

          <section className="results-panel" aria-live="polite" aria-label="Generated edit settings">
            {analysis.isPending ? (
              <div className="results-loading" role="status" aria-label="Generating settings">
                <span className="loading-spinner large" aria-hidden="true" />
                <p className="eyebrow">Generating</p>
                <h2>Reading the image.</h2>
                <p>Building a settings list.</p>
              </div>
            ) : analysis.data ? (
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
                    Use AgX as the display transform, then make only the listed module tweaks.
                  </p>
                ) : null}
                <div className="settings-list">
                  {activeSettings.map((setting) => {
                    const settingKey = `${setting.group}-${setting.name}`;
                    return (
                      <article className="setting-card" key={settingKey}>
                        <div>
                          <span className="setting-group">{setting.group}</span>
                          <h3>{setting.name}</h3>
                          <p>{setting.rationale}</p>
                        </div>
                        <div className="setting-value">
                          <strong>{setting.value}</strong>
                          <button
                            className={copiedSettingKey === settingKey ? "copied-button" : undefined}
                            type="button"
                            onClick={() => void copySetting(`${setting.group} / ${setting.name}: ${setting.value}`, settingKey)}
                          >
                            {copiedSettingKey === settingKey ? "Copied" : "Copy"}
                          </button>
                        </div>
                      </article>
                    );
                  })}
                </div>
                {settingsMode === "lightroom" && lightroomColorControls ? (
                  <LightroomColorControls controls={lightroomColorControls} />
                ) : null}
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

type LightroomColorControlsModel = {
  temperature: {
    value: number;
    position: string;
  };
  tint: {
    value: number;
    position: string;
  };
  wheels: ColorWheelValue[];
};

function LightroomColorControls({ controls }: { controls: LightroomColorControlsModel }) {
  return (
    <section className="lightroom-controls" aria-label="Lightroom color controls">
      <div className="color-slider-grid">
        <ColorSpectrumSlider
          label="Temperature"
          value={`${controls.temperature.value} K`}
          meterLabel="Temperature color position"
          min={TEMPERATURE_MIN}
          max={TEMPERATURE_MAX}
          valueNow={controls.temperature.value}
          markerPosition={controls.temperature.position}
          spectrumClassName="temperature-spectrum"
        />
        <ColorSpectrumSlider
          label="Tint"
          value={formatSignedValue(controls.tint.value)}
          meterLabel="Tint color position"
          min={TINT_MIN}
          max={TINT_MAX}
          valueNow={controls.tint.value}
          markerPosition={controls.tint.position}
          spectrumClassName="tint-spectrum"
        />
      </div>

      {controls.wheels.length > 0 ? (
        <div className="hsl-wheel-grid" aria-label="Lightroom HSL color grading wheels">
          {controls.wheels.map((wheel) => (
            <article className="hsl-wheel-card" key={wheel.name}>
              <div
                className="hsl-wheel"
                role="img"
                aria-label={`${wheel.name} HSL wheel`}
                style={
                  {
                    "--marker-x": wheel.markerX,
                    "--marker-y": wheel.markerY,
                    "--marker-hue": `${wheel.hue}deg`,
                    "--marker-saturation": `${wheel.saturation}%`,
                    "--marker-luminance": `${wheel.luminance}%`
                  } as CSSProperties
                }
              >
                <span className="hsl-wheel-marker" />
              </div>
              <div className="hsl-wheel-meta">
                <h3>{wheel.name}</h3>
                <p>
                  H {wheel.hue} / S {wheel.saturation} / L {formatSignedValue(wheel.luminance)}
                </p>
              </div>
            </article>
          ))}
        </div>
      ) : null}
    </section>
  );
}

function ColorSpectrumSlider({
  label,
  value,
  meterLabel,
  min,
  max,
  valueNow,
  markerPosition,
  spectrumClassName
}: {
  label: string;
  value: string;
  meterLabel: string;
  min: number;
  max: number;
  valueNow: number;
  markerPosition: string;
  spectrumClassName: string;
}) {
  return (
    <article className="color-slider">
      <div className="color-slider-header">
        <h3>{label}</h3>
        <strong>{value}</strong>
      </div>
      <div
        className={`color-spectrum ${spectrumClassName}`}
        role="meter"
        aria-label={meterLabel}
        aria-valuemin={min}
        aria-valuemax={max}
        aria-valuenow={valueNow}
        style={{ "--marker-position": markerPosition } as CSSProperties}
      >
        <span className="spectrum-marker" />
      </div>
    </article>
  );
}

function getLightroomColorControls(settings: EditSetting[]): LightroomColorControlsModel | null {
  const temperature = parseTemperature(readSettingValue(settings, "Temperature"));
  const tint = parseSignedNumber(readSettingValue(settings, "Tint"));
  const wheels = (["Shadows", "Midtones", "Highlights"] as const)
    .map((name) => parseColorWheelValue(name, readColorGradingValue(settings, name)))
    .filter((value): value is ColorWheelValue => Boolean(value));

  if (temperature === null || tint === null) {
    return null;
  }

  return {
    temperature: {
      value: temperature,
      position: percentageInRange(temperature, TEMPERATURE_MIN, TEMPERATURE_MAX)
    },
    tint: {
      value: tint,
      position: percentageInRange(tint, TINT_MIN, TINT_MAX)
    },
    wheels
  };
}

function readSettingValue(settings: EditSetting[], name: string): string | null {
  return settings.find((setting) => setting.name.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.value ?? null;
}

function readColorGradingValue(settings: EditSetting[], name: string): string | null {
  return (
    settings.find(
      (setting) =>
        setting.group.localeCompare("Color Grading", undefined, { sensitivity: "accent" }) === 0 &&
        setting.name.localeCompare(name, undefined, { sensitivity: "accent" }) === 0
    )?.value ?? null
  );
}

function parseTemperature(value: string | null): number | null {
  const match = value?.match(/(\d{4,5})\s?K/i);
  return match ? Number(match[1]) : null;
}

function parseSignedNumber(value: string | null): number | null {
  const match = value?.match(/[+-]?\d+(?:\.\d+)?/);
  return match ? Number(match[0]) : null;
}

function parseColorWheelValue(name: ColorWheelValue["name"], value: string | null): ColorWheelValue | null {
  if (!value) {
    return null;
  }

  const hueValue = readComponentNumber(value, ["hue", "h"]);
  const saturationValue = readComponentNumber(value, ["saturation", "sat", "s"]);
  const luminanceValue = readComponentNumber(value, ["luminance", "lum", "l"]);
  const fallbackValues = value.match(/[+-]?\d+(?:\.\d+)?/g)?.map(Number) ?? [];
  const hue = clamp(hueValue ?? fallbackValues[0], 0, 360);
  const saturation = clamp(saturationValue ?? fallbackValues[1], 0, 100);
  const luminance = clamp(luminanceValue ?? fallbackValues[2], -100, 100);

  if ([hue, saturation, luminance].some((component) => Number.isNaN(component))) {
    return null;
  }

  const radius = (saturation / 100) * 42;
  const angle = (hue * Math.PI) / 180;
  const markerX = formatPercentage(50 + Math.cos(angle) * radius);
  const markerY = formatPercentage(50 + Math.sin(angle) * radius);

  return {
    name,
    hue,
    saturation,
    luminance,
    markerX,
    markerY
  };
}

function readComponentNumber(value: string, labels: string[]): number | null {
  for (const label of labels) {
    const match = value.match(new RegExp(`\\b${label}\\b\\s*(?:[:=]|is)?\\s*([+-]?\\d+(?:\\.\\d+)?)`, "i"));
    if (match) {
      return Number(match[1]);
    }
  }

  return null;
}

function percentageInRange(value: number, min: number, max: number): string {
  return formatPercentage(((clamp(value, min, max) - min) / (max - min)) * 100);
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function formatPercentage(value: number): string {
  return `${Number(value.toFixed(2))}%`;
}

function formatSignedValue(value: number): string {
  return value > 0 ? `+${value}` : `${value}`;
}

export default App;
