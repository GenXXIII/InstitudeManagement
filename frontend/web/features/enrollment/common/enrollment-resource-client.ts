import { request } from "@/lib/http";
import type { EnrollmentItem } from "./enrollment-types";

export type EnrollmentResourceClient = {
  get: (search?: string, departmentId?: string, year?: string) => Promise<EnrollmentItem[]>;
  update: (id: string, values: Record<string, string>, signal?: AbortSignal) => Promise<EnrollmentItem>;
  remove: (id: string) => Promise<void>;
};

export function enrollmentResourceClient(resource: string): EnrollmentResourceClient {
  const path = `/api/enrollment/${resource}`;
  return {
    get: (search = "", departmentId = "", year = "") =>
      request<EnrollmentItem[]>(`${path}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}${year ? `&year=${encodeURIComponent(year)}` : ""}`),
    update: (id, values, signal) => request<EnrollmentItem>(`${path}/${id}`, { method: "PUT", body: JSON.stringify(values), signal }),
    remove: id => request<void>(`${path}/${id}`, { method: "DELETE" }),
  };
}
