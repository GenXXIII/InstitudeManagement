import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";
import type { SemesterResult } from "./result-types";

export type ResultPublicationReadiness = { total: number; ready: number; draft: number; published: number; canPublishAll: boolean };

export const resultApi = {
  get(departmentId = "", year = "", history = false) {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    params.set("history", String(history));
    return requestAllPages<SemesterResult>("/api/results", params);
  },
  getPage(departmentId = "", year = "", history = false, search = "", outcome = "all", options: PageOptions = {}) {
    const params = new URLSearchParams();
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    params.set("history", String(history));
    if (search) params.set("search", search);
    if (outcome !== "all") params.set("outcome", outcome);
    appendPage(params, options);
    return request<PagedResult<SemesterResult>>(`/api/results?${params}`);
  },
  getPublicationReadiness: () => request<ResultPublicationReadiness>("/api/results/publication-readiness"),
  publishAll: () => request<{ published: number }>("/api/results/publish-all", { method: "POST" }),
};
