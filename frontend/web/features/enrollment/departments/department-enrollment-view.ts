import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const departmentEnrollmentCopy: EnrollmentCopy = {
  title: "Department Assign",
  description: "Read-only view of what is assigned to each department for the selected year.",
  columns: ["DepartmentCode", "Department", "Year", "Students", "Teachers", "Courses", "Classrooms", "Weekly classes"],
};

export function departmentEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.departmentCode, value.name, value.year === "All" ? "All years" : `Year ${value.year}`, value.students, value.teachers, value.courses, value.classrooms, value.weeklyClasses];
}
