import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const teacherAssignmentCopy: EnrollmentCopy = {
  title: "Teacher Assign",
  description: "Read-only teachers from matching Timetable Enrollment rows. Public ID is the teacher's mobile login ID.",
  columns: ["EnrollmentCode", "Public ID", "Teacher", "Department", "Assigned courses", "Year levels", "Academic year", "Semester", "Period state", "Create At"],
};

export function teacherAssignmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.enrollmentCode, value.publicId, value.name, value.department, value.courses || `${value.courseCount || 0} assigned`, value.yearLevels || "Not scheduled", value.academicYear, value.semester, value.periodState || "Current", value.createAt];
}
