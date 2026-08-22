import { request } from "@/lib/http";
import type { OperationalRecord } from "./record-types";

export const recordApi = {
  get: (module: string, search = "", departmentId = "", history = false) => request<OperationalRecord[]>(`/api/operational-records/${module}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}${history ? "&history=true" : ""}`),
};
