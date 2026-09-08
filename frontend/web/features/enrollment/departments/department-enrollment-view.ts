import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const departmentEnrollmentCopy: EnrollmentCopy = {
  title: "Department Assign",
  description: "Read-only view of the permanent departments available to Enrollment.",
  columns: ["DepartmentCode", "Department", "Create At"],
};

export function departmentEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.departmentCode, value.name, value.createAt];
}
