import { request } from "@/lib/http";
import type { GradeAssessment } from "./assessment-types";

export const assessmentApi = {
  get(departmentId = "") {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    return request<GradeAssessment[]>(`/api/catalog/grades${params.size ? `?${params}` : ""}`);
  },
  reviewCourse(id: string, decision: "Approved" | "Rejected", note = "") {
    return request<void>(`/api/grades/${id}/review`, { method: "PUT", body: JSON.stringify({ decision, note }) });
  },
};
