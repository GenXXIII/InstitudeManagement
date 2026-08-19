import type { CatalogItem, Dashboard, Operation, RecordItem, Settings } from "./types";

export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
    cache: "no-store",
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed (${response.status})`);
  }
  return response.status === 204 ? (undefined as T) : response.json();
}

export const api = {
  dashboard: () => request<Dashboard>("/api/dashboard"),
  operation: (module: string, departmentId = "") => request<Operation>(`/api/operations/${module}${departmentId ? `?departmentId=${encodeURIComponent(departmentId)}` : ""}`),
  records: (search = "", type = "all") => request<RecordItem[]>(`/api/records?search=${encodeURIComponent(search)}&type=${encodeURIComponent(type)}`),
  catalog: (resource: string, search = "", departmentId = "") => request<CatalogItem[]>(`/api/catalog/${resource}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}`),
  create: (resource: string, values: Record<string, string>) => request<CatalogItem>(`/api/catalog/${resource}`, { method: "POST", body: JSON.stringify(values) }),
  update: (resource: string, id: string, values: Record<string, string>) => request<CatalogItem>(`/api/catalog/${resource}/${id}`, { method: "PUT", body: JSON.stringify(values) }),
  remove: (resource: string, id: string) => request<void>(`/api/catalog/${resource}/${id}`, { method: "DELETE" }),
  settings: (section: string) => request<Settings>(`/api/settings/${section}`),
  saveSettings: (section: string, values: Record<string, string>) => request<Settings>(`/api/settings/${section}`, { method: "PUT", body: JSON.stringify(values) }),
};
