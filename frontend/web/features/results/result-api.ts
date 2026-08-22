import { request } from "@/lib/http";
import type { SemesterResult } from "./result-types";

export const resultApi = {
  get(departmentId = "", year = "") {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    params.set("history", "true");
    return request<SemesterResult[]>(`/api/results?${params}`);
  },
};
