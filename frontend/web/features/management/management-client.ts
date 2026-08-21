import { request } from "@/lib/http";

export function managementResourceClient<TItem>(resource: string) {
  return {
    get: (search = "", departmentId = "") => request<TItem[]>(`/api/catalog/${resource}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}`),
    create: (values: Record<string, string>) => request<TItem>(`/api/catalog/${resource}`, { method: "POST", body: JSON.stringify(values) }),
    update: (id: string, values: Record<string, string>) => request<TItem>(`/api/catalog/${resource}/${id}`, { method: "PUT", body: JSON.stringify(values) }),
    remove: (id: string) => request<void>(`/api/catalog/${resource}/${id}`, { method: "DELETE" }),
  };
}
