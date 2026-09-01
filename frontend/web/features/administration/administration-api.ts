import { request } from "@/lib/http";
import type { SettingSection, Settings } from "./administration-types";

export const administrationApi = {
  list: (signal?: AbortSignal) => request<Settings[]>("/api/settings", { signal }),
  get: (section: SettingSection, signal?: AbortSignal) => request<Settings>(`/api/settings/${section}`, { signal }),
  save: (section: SettingSection, values: Record<string, string>, signal?: AbortSignal) => request<Settings>(`/api/settings/${section}`, { method: "PUT", body: JSON.stringify(values), signal }),
  uploadAsset: (kind: "logo" | "favicon", file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<{ url: string; path: string; fileName: string }>(`/api/settings/assets/${kind}`, { method: "POST", body: form });
  },
};
