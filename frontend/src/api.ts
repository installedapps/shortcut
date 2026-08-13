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

  return response.json();
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
