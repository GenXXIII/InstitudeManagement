import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const timetableEnrollmentCopy: EnrollmentCopy = {
  title: "Timetable Enrollment",
  description: "Select a permanent Management schedule, then assign its current-semester course, teacher, classroom, and student year.",
  columns: ["EnrollmentCode", "Day / time", "Year", "Academic year", "Semester", "Shift", "Course", "Classroom", "Teacher", "Department", "Status", "Period state", "Create At", "Actions"],
};

export function timetableEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [
    value.enrollmentCode,
    `${value.dayOfWeek} ${value.startsAt}-${value.endsAt}`,
    value.yearLevel ? `Year ${value.yearLevel}` : "Unassigned",
    value.academicYear,
    value.semester,
    value.shift,
    value.course,
    value.classroom,
    value.teacher,
    value.department,
    value.classroomStatus || "Maintenance",
    value.periodState || "Current",
    value.createAt,
  ];
}
