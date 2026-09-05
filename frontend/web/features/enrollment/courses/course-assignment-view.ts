import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const courseAssignmentCopy: EnrollmentCopy = {
  title: "Course Assign",
  description: "Read-only view of the department and year assigned to each course.",
  columns: ["CourseCode", "Course", "Department", "Year"],
};

export function courseAssignmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.courseCode, value.name, value.department, value.year ? `Year ${value.year}` : "Unassigned"];
}
