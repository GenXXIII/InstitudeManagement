import { request } from "@/lib/http";
import type { Operation } from "./operations-types";

export const operationsApi = {
  get: (module: string, departmentId = "") => request<Operation>(`/api/operations/${module}${departmentId ? `?departmentId=${encodeURIComponent(departmentId)}` : ""}`),
};
