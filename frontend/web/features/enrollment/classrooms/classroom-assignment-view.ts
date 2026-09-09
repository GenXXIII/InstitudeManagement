import type { EnrollmentCopy } from "../common/enrollment-copy";
import type { EnrollmentDisplayItem, EnrollmentItem } from "../common/enrollment-types";

export const classroomAssignmentCopy: EnrollmentCopy = {
  title: "Classroom Assign",
  description: "Read-only classrooms and courses from Timetable Enrollment rows that match an active Student Enrollment cohort.",
  columns: ["EnrollmentCode", "Classroom", "Access", "Assigned course", "Capacity", "Status", "Academic year", "Semester", "Period state", "Create At"],
};

export function classroomAssignmentDisplayItems(items: EnrollmentItem[]): EnrollmentDisplayItem[] {
  return items.flatMap(item => {
    const assignedCourses = (item.values.courses?.split(",").map(course => course.trim()).filter(Boolean) ?? [])
      .toSorted((left, right) => left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }));
    return (assignedCourses.length ? assignedCourses : ["Not scheduled"]).map((assignedCourse, index) => ({
      ...item,
      assignedCourse,
      rowKey: `${item.id}-${item.values.academicYear}-${item.values.semester}-${index}-${assignedCourse}`,
    }));
  });
}

export function classroomAssignmentCells(item: EnrollmentDisplayItem) {
  const value = item.values;
  return [value.enrollmentCode, `${value.building} - ${value.roomType}`, value.access, item.assignedCourse || "Not scheduled", value.capacity ? `${value.capacity} seats` : "Unassigned", value.status || "Available", value.academicYear, value.semester, value.periodState || "Current", value.createAt];
}

export function classroomEnrollmentStatusClass(status: string) {
  if (status === "Maintenance") return "starting";
  if (status === "Unavailable") return "offline";
  return "available";
}
