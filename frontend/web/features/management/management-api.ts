import { request } from "@/lib/http";
import type { CatalogItem } from "./management-types";

export const managementApi = {
  get: (resource: string, search = "", departmentId = "") => request<CatalogItem[]>(`/api/catalog/${resource}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}`),
  create: (resource: string, values: Record<string, string>) => request<CatalogItem>(`/api/catalog/${resource}`, { method: "POST", body: JSON.stringify(values) }),
  update: (resource: string, id: string, values: Record<string, string>) => request<CatalogItem>(`/api/catalog/${resource}/${id}`, { method: "PUT", body: JSON.stringify(values) }),
  remove: (resource: string, id: string) => request<void>(`/api/catalog/${resource}/${id}`, { method: "DELETE" }),
};
