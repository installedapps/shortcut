export type EditSetting = {
  group: string;
  name: string;
  value: string;
  rationale: string;
};

export type AnalysisResponse = {
  id: string;
  fileName: string;
  createdAt: string;
  summary: string;
  lightroomSettings: EditSetting[];
  darktableSettings: EditSetting[];
};

export async function createAnalysis(photo: File): Promise<AnalysisResponse> {
  const form = new FormData();
  form.append("photo", photo);

  const response = await fetch("/api/analyses", {
    method: "POST",
    body: form
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(readErrorMessage(error) || "The photograph could not be analyzed.");
  }

  let body: unknown;
  try {
    body = await response.json();
  } catch {
    throw new Error("The analysis response was malformed. Try generating settings again.");
  }

  if (!isAnalysisResponse(body) || !hasValidLightroomValues(body.lightroomSettings)) {
    throw new Error("The analysis response included invalid Lightroom values. Try generating settings again.");
  }

  return body;
}

function readErrorMessage(error: string): string {
  if (!error) {
    return "";
  }

  try {
    const problem = JSON.parse(error) as { detail?: string; title?: string };
    return problem.detail || problem.title || error;
  } catch {
    return error;
  }
}

function isAnalysisResponse(value: unknown): value is AnalysisResponse {
  if (!value || typeof value !== "object") {
    return false;
  }

  const analysis = value as Partial<AnalysisResponse>;
  return (
    typeof analysis.id === "string" &&
    typeof analysis.fileName === "string" &&
    typeof analysis.createdAt === "string" &&
    typeof analysis.summary === "string" &&
    Array.isArray(analysis.lightroomSettings) &&
    Array.isArray(analysis.darktableSettings) &&
    analysis.lightroomSettings.every(isEditSetting) &&
    analysis.darktableSettings.every(isEditSetting)
  );
}

function isEditSetting(value: unknown): value is EditSetting {
  if (!value || typeof value !== "object") {
    return false;
  }

  const setting = value as Partial<EditSetting>;
  return (
    typeof setting.group === "string" &&
    typeof setting.name === "string" &&
    typeof setting.value === "string" &&
    typeof setting.rationale === "string"
  );
}

function hasValidLightroomValues(settings: EditSetting[]): boolean {
  const findSetting = (name: string) =>
    settings.find((setting) => setting.name.localeCompare(name, undefined, { sensitivity: "accent" }) === 0);
  const hasColorGrading = (name: string) =>
    settings.some(
      (setting) =>
        setting.group.localeCompare("Color Grading", undefined, { sensitivity: "accent" }) === 0 &&
        setting.name.localeCompare(name, undefined, { sensitivity: "accent" }) === 0
    );

  const temperature = findSetting("Temperature");
  const tint = findSetting("Tint");
  const vibrance = findSetting("Vibrance");
  const saturation = findSetting("Saturation");
  const kelvinValue = /^\d{4,5}\s?K$/i;
  const signedValue = /^[+-]\d+(?:\.\d+)?%?$/;

  return (
    Boolean(temperature && kelvinValue.test(temperature.value.trim())) &&
    Boolean(tint && signedValue.test(tint.value.trim())) &&
    Boolean(vibrance && signedValue.test(vibrance.value.trim())) &&
    Boolean(saturation && signedValue.test(saturation.value.trim())) &&
    hasColorGrading("Shadows") &&
    hasColorGrading("Midtones") &&
    hasColorGrading("Highlights")
  );
}
