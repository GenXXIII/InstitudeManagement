import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const timetableEnrollmentCopy: EnrollmentCopy = {
  title: "Timetable Enrollment",
  description: "Select a permanent Management schedule, then assign its current-semester course, teacher, classroom, and student year.",
  columns: ["EnrollmentCode", "Course", "Teacher", "Department", "Year", "Semester", "Classroom", "Shift", "Day / time", "Status", "Create At", "Actions"],
};

export function timetableEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.enrollmentCode, [value.courseCode, value.course].filter(Boolean).join(" - "), [value.teacherCode, value.teacher].filter(Boolean).join(" - "), value.department, value.yearLevel ? `Year ${value.yearLevel}` : "Unassigned", value.semester, value.classroom, value.shift, `${value.dayOfWeek} ${value.startsAt}-${value.endsAt}`, value.classroomStatus || "Maintenance", value.createAt];
}
