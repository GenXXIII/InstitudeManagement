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
  publishAll: () => request<{ published: number }>("/api/results/publish-all", { method: "POST" }),
};
