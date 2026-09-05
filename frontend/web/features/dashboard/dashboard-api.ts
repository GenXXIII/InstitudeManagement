import { request } from "@/lib/http";
import type { Dashboard, DashboardRange } from "./dashboard-types";

export const dashboardApi = { get: (range: DashboardRange) => request<Dashboard>(`/api/dashboard?range=${range}`) };
