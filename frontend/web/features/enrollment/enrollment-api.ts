import { request } from "@/lib/http";

export type EnrollmentResource = "students" | "teachers" | "courses" | "classrooms" | "timetable" | "departments";
export type EnrollmentItem = { id: string; values: Record<string, string> };

export const enrollmentApi = {
  get: (resource: EnrollmentResource, search = "", departmentId = "", year = "") => request<EnrollmentItem[]>(`/api/enrollment/${resource}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}${year ? `&year=${encodeURIComponent(year)}` : ""}`),
  update: (resource: EnrollmentResource, id: string, values: Record<string, string>, signal?: AbortSignal) => request<EnrollmentItem>(`/api/enrollment/${resource}/${id}`, { method: "PUT", body: JSON.stringify(values), signal }),
  remove: (resource: EnrollmentResource, id: string) => request<void>(`/api/enrollment/${resource}/${id}`, { method: "DELETE" }),
};
