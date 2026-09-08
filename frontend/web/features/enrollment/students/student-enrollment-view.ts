import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const studentEnrollmentCopy: EnrollmentCopy = {
  title: "Student Enrollment",
  description: "Select a Management student, then choose any department and Year 1-4. The linked EnrollmentCode is generated automatically from StudentCode.",
  columns: ["EnrollmentCode", "Name", "Year", "Shift", "Department", "Semester", "Create At", "Actions"],
};

export const studentAssignmentCopy: EnrollmentCopy = {
  title: "Student Assign",
  description: "Read-only view of each enrolled student's department, student year, learning shift, and creation date.",
  columns: ["EnrollmentCode", "Student", "Department", "Year", "Shift", "Create At"],
};

export function studentEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [
    value.enrollmentCode,
    value.name,
    value.year ? `Year ${value.year}` : "Unassigned",
    value.shift || "Unassigned",
    value.department,
    value.semester,
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
    value.shift || "Unassigned",
    value.createAt,
  ];
}
