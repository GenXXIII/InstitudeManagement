import { request } from "@/lib/http";
import type { Dashboard } from "./dashboard-types";

export const dashboardApi = { get: () => request<Dashboard>("/api/dashboard") };
