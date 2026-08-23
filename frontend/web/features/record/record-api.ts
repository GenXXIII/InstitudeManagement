import { request } from "@/lib/http";
import type { ClassSessionAttendanceUpdate, OperationalRecord } from "./record-types";

export const recordApi = {
  get: (module: string, search = "", departmentId = "", history = false) => request<OperationalRecord[]>(`/api/operational-records/${module}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}${history ? "&history=true" : ""}`),
  updateSession: (id: string, students: ClassSessionAttendanceUpdate[]) => request<void>(`/api/operational-records/sessions/${id}`, { method: "PUT", body: JSON.stringify({ students }) }),
};
