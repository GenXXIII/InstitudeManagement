import { request } from "@/lib/http";
import type { Settings } from "./administration-types";

export const administrationApi = {
  get: (section: string) => request<Settings>(`/api/settings/${section}`),
  save: (section: string, values: Record<string, string>) => request<Settings>(`/api/settings/${section}`, { method: "PUT", body: JSON.stringify(values) }),
};
