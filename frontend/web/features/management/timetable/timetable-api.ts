import { managementResourceClient } from "../management-client";
import { request } from "@/lib/http";
import type { TimetableItem, TimetablePeriod } from "../types/timetable";

export const timetableApi = {
  ...managementResourceClient<TimetableItem>("timetable"),
  getPeriods: () => request<TimetablePeriod[]>("/api/timetable/periods"),
};
