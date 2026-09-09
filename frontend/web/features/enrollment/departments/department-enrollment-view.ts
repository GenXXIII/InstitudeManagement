import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const departmentEnrollmentCopy: EnrollmentCopy = {
  title: "Department Assign",
  description: "Read-only departments produced by matching Student Enrollment and Timetable Enrollment cohorts.",
  columns: ["DepartmentCode", "Department", "Students", "Timetables", "Year levels", "Academic year", "Semester", "Period state", "Create At"],
};

export function departmentEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.departmentCode, value.name, value.studentCount, value.timetableCount, value.yearLevels, value.academicYear, value.semester, value.periodState || "Current", value.createAt];
}
