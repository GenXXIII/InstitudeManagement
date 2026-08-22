export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

type ApiProblem = {
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
};

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_URL}${path}`, { ...init, headers: { "Content-Type": "application/json", ...init?.headers }, cache: "no-store" });
  } catch (reason) {
    if (init?.signal?.aborted) throw reason;
    throw new Error("Cannot connect to the INK API. Check that INK-API is running, then try again.");
  }
  if (!response.ok) {
    const body = await response.text();
    const problem = (() => { try { return JSON.parse(body) as ApiProblem; } catch { return null; } })();
    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat().join(" ")
      : undefined;
    throw new Error(validationMessage ?? problem?.detail ?? problem?.title ?? (body || `Request failed (${response.status})`));
  }
  return response.status === 204 ? (undefined as T) : response.json();
}
