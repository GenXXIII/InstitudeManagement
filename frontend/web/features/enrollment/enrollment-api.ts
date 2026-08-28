import { request } from "@/lib/http";

export type EnrollmentResource = "students" | "student-assignments" | "teachers" | "courses" | "classrooms" | "timetable" | "departments";
export type EnrollmentItem = { id: string; values: Record<string, string> };

function apiResource(resource: EnrollmentResource) {
  return resource === "student-assignments" ? "students" : resource;
}

export const enrollmentApi = {
  get: (resource: EnrollmentResource, search = "", departmentId = "", year = "") => request<EnrollmentItem[]>(`/api/enrollment/${apiResource(resource)}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}${year ? `&year=${encodeURIComponent(year)}` : ""}`),
  update: (resource: EnrollmentResource, id: string, values: Record<string, string>, signal?: AbortSignal) => request<EnrollmentItem>(`/api/enrollment/${apiResource(resource)}/${id}`, { method: "PUT", body: JSON.stringify(values), signal }),
  remove: (resource: EnrollmentResource, id: string) => request<void>(`/api/enrollment/${apiResource(resource)}/${id}`, { method: "DELETE" }),
};
