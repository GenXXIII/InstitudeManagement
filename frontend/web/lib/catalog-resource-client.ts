import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";

export function catalogResourceClient<TItem>(resource: string, serverPaged = false) {
  const path = `/api/catalog/${resource}`;
  const parameters = (search = "", departmentId = "") => {
    const params = new URLSearchParams({ search });
    if (departmentId) params.set("departmentId", departmentId);
    return params;
  };
  return {
    get: (search = "", departmentId = "", signal?: AbortSignal) => serverPaged ? requestAllPages<TItem>(path, parameters(search, departmentId), signal) : request<TItem[]>(`${path}?${parameters(search, departmentId)}`, { signal }),
    getPage: (search = "", departmentId = "", options: PageOptions = {}, signal?: AbortSignal) => {
      const params = appendPage(parameters(search, departmentId), options);
      return serverPaged
        ? request<PagedResult<TItem>>(`${path}?${params}`, { signal })
        : request<TItem[]>(`${path}?${params}`, { signal }).then(items => ({ items, page: 1, pageSize: items.length || 40, totalCount: items.length, totalPages: items.length ? 1 : 0 }));
    },
    create: (values: Record<string, string>, signal?: AbortSignal) => request<TItem>(path, { method: "POST", body: JSON.stringify(values), signal }),
    update: (id: string, values: Record<string, string>, signal?: AbortSignal) => request<TItem>(`${path}/${id}`, { method: "PUT", body: JSON.stringify(values), signal }),
    remove: (id: string) => request<void>(`${path}/${id}`, { method: "DELETE" }),
  };
}
