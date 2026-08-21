import { managementResourceClient } from "../management-client";
import type { AttendanceItem } from "../types/attendance";

export const attendanceApi = managementResourceClient<AttendanceItem>("attendance");
