import { catalogResourceClient } from "@/lib/catalog-resource-client";
import { request } from "@/lib/http";
import type { TimetableItem, TimetablePeriod } from "./timetable-types";

export const timetableApi = {
  ...catalogResourceClient<TimetableItem>("timetable"),
  getPeriods: () => request<TimetablePeriod[]>("/api/timetable/periods"),
};
