import {
  classroomAssignmentCells,
  classroomAssignmentCopy,
  classroomAssignmentDisplayItems,
  classroomEnrollmentStatusClass,
} from "./classrooms/classroom-assignment-view";
import type { EnrollmentCopy } from "./common/enrollment-copy";
import type { EnrollmentDisplayItem, EnrollmentItem, EnrollmentResource } from "./common/enrollment-types";
import { courseAssignmentCells, courseAssignmentCopy } from "./courses/course-assignment-view";
import { departmentEnrollmentCells, departmentEnrollmentCopy } from "./departments/department-enrollment-view";
import {
  studentAssignmentCells,
  studentAssignmentCopy,
  studentEnrollmentCells,
  studentEnrollmentCopy,
} from "./students/student-enrollment-view";
import { teacherAssignmentCells, teacherAssignmentCopy } from "./teachers/teacher-assignment-view";
import { timetableEnrollmentCells, timetableEnrollmentCopy } from "./timetable/timetable-enrollment-view";

export type SelectableEnrollmentResource = "students" | "timetable";

export const enrollmentCopy: Record<EnrollmentResource, EnrollmentCopy> = {
  students: studentEnrollmentCopy,
  "student-assignments": studentAssignmentCopy,
  teachers: teacherAssignmentCopy,
  courses: courseAssignmentCopy,
  classrooms: classroomAssignmentCopy,
  timetable: timetableEnrollmentCopy,
  departments: departmentEnrollmentCopy,
};

const assignedOnlyResources = new Set<EnrollmentResource>([
  "students",
  "student-assignments",
  "teachers",
  "courses",
  "classrooms",
]);

const resourceSubjects: Record<EnrollmentResource, string> = {
  students: "student enrollment",
  "student-assignments": "student enrollment",
  teachers: "teacher",
  courses: "course",
  classrooms: "classroom",
  timetable: "timetable",
  departments: "department",
};

export function isEditableEnrollment(resource: EnrollmentResource) {
  return resource === "students" || resource === "timetable";
}

export function isSelectableEnrollment(resource: EnrollmentResource): resource is SelectableEnrollmentResource {
  return resource === "students" || resource === "timetable";
}

export function buildEnrollmentDisplayItems(items: EnrollmentItem[], resource: EnrollmentResource): EnrollmentDisplayItem[] {
  const assignedItems = assignedOnlyResources.has(resource)
    ? items.filter(item => item.values.status !== "Unassigned")
    : items;
  return resource === "classrooms"
    ? classroomAssignmentDisplayItems(assignedItems)
    : assignedItems.map(item => ({ ...item, rowKey: item.id }));
}

export function sortEnrollmentItems<T extends EnrollmentItem>(items: T[], resource: EnrollmentResource) {
  return items.toSorted((left, right) => {
    const yearDifference = enrollmentYear(left, resource) - enrollmentYear(right, resource);
    if (yearDifference) return yearDifference;
    const codeDifference = enrollmentCode(left).localeCompare(enrollmentCode(right), undefined, { numeric: true, sensitivity: "base" });
    if (codeDifference) return codeDifference;
    return assignedCourseName(left).localeCompare(assignedCourseName(right), undefined, { numeric: true, sensitivity: "base" });
  });
}

export function enrollmentSubject(resource: EnrollmentResource) {
  return resourceSubjects[resource];
}

export function enrollmentCells(resource: EnrollmentResource, item: EnrollmentDisplayItem, studentSchedules: EnrollmentItem[]) {
  if (resource === "students") return studentEnrollmentCells(item);
  if (resource === "student-assignments") return studentAssignmentCells(item, studentSchedules);
  if (resource === "teachers") return teacherAssignmentCells(item);
  if (resource === "courses") return courseAssignmentCells(item);
  if (resource === "classrooms") return classroomAssignmentCells(item);
  if (resource === "timetable") return timetableEnrollmentCells(item);
  return departmentEnrollmentCells(item);
}

export { classroomEnrollmentStatusClass };

function enrollmentYear(item: EnrollmentItem, resource: EnrollmentResource) {
  const value = resource === "timetable" ? item.values.yearLevel : item.values.year || item.values.yearLevels;
  const match = value?.match(/\d+/);
  return match ? Number(match[0]) : 99;
}

function enrollmentCode(item: EnrollmentItem) {
  const values = item.values;
  return values.enrollmentCode || values.studentCode || values.teacherCode || values.courseCode || values.classroomCode || values.timetableCode || values.departmentCode || item.id;
}

function assignedCourseName(item: EnrollmentItem) {
  return "assignedCourse" in item && typeof item.assignedCourse === "string" ? item.assignedCourse : "";
}
