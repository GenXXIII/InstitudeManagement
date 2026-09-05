import { catalogResourceClient } from "@/lib/catalog-resource-client";
import type { AttendanceItem } from "./attendance-types";

export const attendanceApi = catalogResourceClient<AttendanceItem>("attendance");
