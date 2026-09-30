import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";
import type { GradeAssessment } from "./assessment-types";

export const assessmentApi = {
  get(departmentId = "", year = "") {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    return requestAllPages<GradeAssessment>("/api/catalog/grades", params);
  },
  getPage(departmentId = "", year = "", search = "", groupBy: "student" | "course" | "" = "", status = "", options: PageOptions = {}) {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    if (search) params.set("search", search);
    if (groupBy) params.set("groupBy", groupBy);
    if (status && status !== "All") params.set("status", status);
    appendPage(params, options);
    return request<PagedResult<GradeAssessment>>(`/api/catalog/grades?${params}`);
  },
  reviewCourse(id: string, decision: "Approved" | "Rejected", note = "") {
    return request<void>(`/api/grades/${id}/review`, { method: "PUT", body: JSON.stringify({ decision, note }) });
  },
  confirmFinalGrades(departmentId = "", year = "") {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    return request<{ confirmed: number }>(`/api/grades/final-results/confirm-all${params.size ? `?${params}` : ""}`, { method: "POST" });
  },
};
