import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem, EnrollmentItem } from "../common/enrollment-types";
import { scheduleMatchesShift } from "../common/enrollment-relationships";

export const studentEnrollmentCopy: EnrollmentCopy = {
  title: "Student Enrollment",
  description: "Select a student added in Management, then enroll their code and name into a department, year, and learning shift.",
  columns: ["StudentCode", "Name", "Year", "Shift", "Department", "Actions"],
};

export const studentAssignmentCopy: EnrollmentCopy = {
  title: "Student Assign",
  description: "Read-only view of each enrolled student's department, year, shift, assigned courses, classrooms, and weekly classes.",
  columns: ["StudentCode", "Student", "Department", "Year / shift", "Assigned courses", "Assigned classrooms", "Weekly classes"],
};

export function studentEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.studentCode, value.name, value.year ? `Year ${value.year}` : "Unassigned", value.shift || "Unassigned", value.year === "1" ? "General foundation" : value.department];
}

export function studentAssignmentCells(item: EnrollmentDisplayItem, schedules: EnrollmentItem[]) {
  const value = item.values;
  const relatedSchedules = schedules.filter(schedule =>
    schedule.values.departmentId === value.departmentId
    && schedule.values.yearLevel === value.year
    && scheduleMatchesShift(schedule, value.shift));
  return [
    value.studentCode,
    value.name,
    value.year === "1" ? "General foundation" : value.department,
    [value.year ? `Year ${value.year}` : "Unassigned", value.shift].filter(Boolean).join(" / "),
    uniqueValues(relatedSchedules, "course"),
    uniqueValues(relatedSchedules, "classroom"),
    relatedSchedules.length.toString(),
  ];
}

function uniqueValues(items: EnrollmentItem[], key: string) {
  return [...new Set(items.map(item => item.values[key]).filter(Boolean))].join(", ") || "Not scheduled";
}
