import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const teacherAssignmentCopy: EnrollmentCopy = {
  title: "Teacher Assign",
  description: "Read-only teachers from Timetable Enrollment rows that match an active Student Enrollment cohort.",
  columns: ["EnrollmentCode", "Teacher", "Department", "Assigned courses", "Year levels", "Create At"],
};

export function teacherAssignmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.enrollmentCode, value.name, value.department, value.courses || `${value.courseCount || 0} assigned`, value.yearLevels || "Not scheduled", value.createAt];
}
