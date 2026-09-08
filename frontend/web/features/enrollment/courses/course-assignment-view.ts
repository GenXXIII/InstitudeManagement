import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const courseAssignmentCopy: EnrollmentCopy = {
  title: "Course Assign",
  description: "Read-only view of the department, permanent student year, and semester assigned to each course.",
  columns: ["EnrollmentCode", "Course", "Department", "Year", "Semester", "Create At"],
};

export function courseAssignmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.enrollmentCode, value.name, value.department, value.year ? `Year ${value.year}` : "Unassigned", value.semester, value.createAt];
}
