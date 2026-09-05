import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem } from "../common/enrollment-types";

export const timetableEnrollmentCopy: EnrollmentCopy = {
  title: "Timetable Enrollment",
  description: "Add and manage enrolled schedules, including the classroom availability status that controls whether a class can run.",
  columns: ["EnrollmentCode", "Course", "Teacher", "Department", "Year", "Classroom", "Day / time", "Status", "Create At", "Actions"],
};

export function timetableEnrollmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.enrollmentCode, [value.courseCode, value.course].filter(Boolean).join(" - "), [value.teacherCode, value.teacher].filter(Boolean).join(" - "), value.department, value.yearLevel ? `Year ${value.yearLevel}` : "Unassigned", value.classroom, `${value.dayOfWeek} ${value.startsAt}-${value.endsAt}`, value.classroomStatus || "Maintenance", value.createAt];
}
