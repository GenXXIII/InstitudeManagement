import { request } from "@/lib/http";
import type { StudentPayment } from "./finance-types";

export const financeApi = {
  get: (search = "", status = "All") => request<StudentPayment[]>(`/api/finance?search=${encodeURIComponent(search)}&status=${encodeURIComponent(status)}`),
};
