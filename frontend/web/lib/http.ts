export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

type ApiProblem = {
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
};

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, { ...init, headers: { "Content-Type": "application/json", ...init?.headers }, cache: "no-store" });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as ApiProblem | null;
    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat().join(" ")
      : undefined;
    throw new Error(validationMessage ?? problem?.detail ?? problem?.title ?? `Request failed (${response.status})`);
  }
  return response.status === 204 ? (undefined as T) : response.json();
}
