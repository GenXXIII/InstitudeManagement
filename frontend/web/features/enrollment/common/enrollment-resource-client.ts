import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";
import type { EnrollmentItem } from "./enrollment-types";

export type EnrollmentResourceClient = {
  get: (search?: string, departmentId?: string, year?: string) => Promise<EnrollmentItem[]>;
  getPage: (search?: string, departmentId?: string, year?: string, options?: PageOptions) => Promise<PagedResult<EnrollmentItem>>;
  update: (id: string, values: Record<string, string>, signal?: AbortSignal) => Promise<EnrollmentItem>;
  remove: (id: string) => Promise<void>;
};

export function enrollmentResourceClient(resource: string): EnrollmentResourceClient {
  const path = `/api/enrollment/${resource}`;
  const isServerPaged = resource !== "departments";
  const parameters = (search = "", departmentId = "", year = "") => {
    const params = new URLSearchParams({ search });
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    return params;
  };
  return {
    get: (search = "", departmentId = "", year = "") => isServerPaged
      ? requestAllPages<EnrollmentItem>(path, parameters(search, departmentId, year))
      : request<EnrollmentItem[]>(`${path}?${parameters(search, departmentId, year)}`),
    getPage: (search = "", departmentId = "", year = "", options = {}) => {
      const params = appendPage(parameters(search, departmentId, year), options);
      return isServerPaged
        ? request<PagedResult<EnrollmentItem>>(`${path}?${params}`)
        : request<EnrollmentItem[]>(`${path}?${params}`).then(items => ({ items, page: 1, pageSize: items.length || 40, totalCount: items.length, totalPages: items.length ? 1 : 0 }));
    },
    update: (id, values, signal) => request<EnrollmentItem>(`${path}/${id}`, { method: "PUT", body: JSON.stringify(values), signal }),
    remove: id => request<void>(`${path}/${id}`, { method: "DELETE" }),
  };
}
