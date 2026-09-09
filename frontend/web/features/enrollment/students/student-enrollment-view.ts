import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const studentEnrollmentCopy: EnrollmentCopy = {
  title: "Student Enrollment",
  description: "Select a Management student, then choose any department and Year 1-4. The linked EnrollmentCode is generated automatically from StudentCode.",
  columns: ["EnrollmentCode", "Name", "Department", "Year", "Semester", "Shift", "Create At", "Actions"],
};

export const studentAssignmentCopy: EnrollmentCopy = {
  title: "Student Assign",
  description: "Read-only students whose Student Enrollment cohort has a matching Timetable Enrollment in the same academic year, semester, department, year, and shift.",
  columns: ["EnrollmentCode", "Student", "Department", "Year", "Semester", "Shift", "Assigned courses", "Create At"],
};

export function studentEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [
    value.enrollmentCode,
    value.name,
    value.department,
    value.year ? `Year ${value.year}` : "Unassigned",
    value.semester,
    value.shift || "Unassigned",
    value.createAt,
  ];
}

export function studentAssignmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [
    value.enrollmentCode,
    value.name,
    value.department,
    value.year ? `Year ${value.year}` : "Unassigned",
    value.semester,
    value.shift || "Unassigned",
    value.courses || "Not scheduled",
    value.createAt,
  ];
}
