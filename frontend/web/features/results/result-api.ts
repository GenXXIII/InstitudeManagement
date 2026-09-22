import { request } from "@/lib/http";
import type { SemesterResult } from "./result-types";

export const resultApi = {
  get(departmentId = "", year = "", history = false) {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    params.set("history", String(history));
    return request<SemesterResult[]>(`/api/results?${params}`);
  },
  publish(row: SemesterResult) {
    return request<void>("/api/results/publish", { method: "POST", body: JSON.stringify({ studentId: row.studentId, academicYear: row.academicYear, semester: row.semester }) });
  },
  publishAll(departmentId = "", year = "") {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    return request<{ published: number }>(`/api/results/publish-all?${params}`, { method: "POST" });
  },
};
