import type { EnrollmentItem } from "./enrollment-api";

export const enrollmentShifts = ["Morning", "Afternoon", "Evening", "Weekend"] as const;

export function scheduleMatchesShift(schedule: EnrollmentItem, shift: string | undefined) {
  if (!shift) return true;
  if (shift === "Weekend") return schedule.values.dayOfWeek === "Saturday" || schedule.values.dayOfWeek === "Sunday";
  if (schedule.values.dayOfWeek === "Saturday" || schedule.values.dayOfWeek === "Sunday") return false;

  const hour = Number(schedule.values.startsAt?.slice(0, 2));
  if (!Number.isFinite(hour)) return false;
  return shift === "Morning" ? hour < 13 : shift === "Afternoon" ? hour >= 13 && hour < 17 : hour >= 17;
}

export function enrollmentCohortKey(departmentId: string, year: string, shift: string) {
  return `${departmentId}:${year}:${shift}`;
}
