import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const studentEnrollmentCopy: EnrollmentCopy = {
  title: "Student Enrollment",
  description: "Select a Management student, then choose any department and Year 1-4. Next Period shows what is still required before the student can move automatically at period end.",
  columns: ["EnrollmentCode", "Name", "Department", "Year", "Academic year", "Semester", "Shift", "Payment", "Next Period", "Create At", "Actions"],
};

export const studentAssignmentCopy: EnrollmentCopy = {
  title: "Student Assign",
  description: "One read-only assignment per student. The Public ID opens all general Year 1 courses, then every current course in the student's chosen major from Year 2 onward.",
  columns: ["EnrollmentCode", "Public ID", "Student", "Department", "Year", "Semester", "Shift", "Create At"],
};

export function studentEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [
    value.enrollmentCode,
    value.name,
    value.department,
    value.year ? `Year ${value.year}` : "Unassigned",
    value.academicYear,
    value.semester,
    value.shift || "Unassigned",
    value.paymentStatus || "Pending",
    value.nextPeriodStatus || "Checking requirements",
    value.createAt,
  ];
}

export function studentAssignmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [
    value.enrollmentCode,
    value.publicId,
    value.name,
    value.department,
    value.year ? `Year ${value.year}` : "Unassigned",
    value.semester,
    value.shift || "Unassigned",
    value.createAt,
  ];
}
