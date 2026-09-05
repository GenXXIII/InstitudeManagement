import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const teacherAssignmentCopy: EnrollmentCopy = {
  title: "Teacher Assign",
  description: "Read-only view of what each teacher is assigned to across departments, courses, year levels, and weekly classes.",
  columns: ["TeacherCode", "Teacher", "Department", "Assigned courses", "Year levels", "Weekly classes"],
};

export function teacherAssignmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.teacherCode, value.name, value.department, value.courses || `${value.courseCount || 0} assigned`, value.yearLevels || "Not scheduled", value.weeklyClasses || "0"];
}
