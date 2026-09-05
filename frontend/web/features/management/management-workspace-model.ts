import { managementCode } from "./management-id";
import type { ManagementItem, ManagementModule, References } from "./management-types";

export function filterManagementItemsByYear(items: ManagementItem[], module: ManagementModule, year: string) {
  if (!year) return items;
  if (module === "timetable") return items.filter(item => item.values.yearLevel === year);
  return items;
}

export function filterManagementReferencesByYear(references: References, year: string): References {
  if (!year) return references;
  const students = references.students.filter(student => student.values.year === year);
  const studentIds = new Set(students.map(student => student.id));
  return {
    ...references,
    students,
    timetable: references.timetable.filter(entry => entry.values.yearLevel === year),
    attendance: references.attendance.filter(item => studentIds.has(item.values.studentId)),
  };
}

export function sortManagementItemsByYear(items: ManagementItem[], module: ManagementModule, references: References) {
  const studentDepartments = new Map<string, number>();
  for (const student of references.students) studentDepartments.set(student.values.departmentId, Math.min(studentDepartments.get(student.values.departmentId) ?? 99, Number(student.values.year)));
  const timetableYear = (field: "teacherId" | "courseId" | "classroomId", id: string) => references.timetable.filter(entry => entry.values[field] === id).reduce((minimum, entry) => Math.min(minimum, Number(entry.values.yearLevel)), 99);
  const yearOf = (item: ManagementItem) => {
    const values = item.values as unknown as Record<string, string>;
    if (values.year || values.yearLevel) return Number(values.year ?? values.yearLevel);
    if (module === "teachers") return timetableYear("teacherId", item.id);
    if (module === "courses") return timetableYear("courseId", item.id);
    if (module === "classrooms") return timetableYear("classroomId", item.id);
    if (module === "departments" || module === "overview") return studentDepartments.get(item.id) ?? 99;
    return 99;
  };
  const businessId = (item: ManagementItem) => {
    const values = item.values as unknown as Record<string, string>;
    return managementCode(module, values) || item.id;
  };
  return items.toSorted((left, right) => yearOf(left) - yearOf(right) || businessId(left).localeCompare(businessId(right), undefined, { numeric: true, sensitivity: "base" }));
}

export function sortManagementReferencesByYear(references: References): References {
  const studentYears = new Map(references.students.map(student => [student.id, Number(student.values.year)]));
  return {
    ...references,
    departments: references.departments.toSorted((left, right) => left.values.departmentCode.localeCompare(right.values.departmentCode, undefined, { numeric: true })),
    teachers: references.teachers.toSorted((left, right) => left.values.teacherCode.localeCompare(right.values.teacherCode, undefined, { numeric: true })),
    students: references.students.toSorted((left, right) => Number(left.values.year) - Number(right.values.year) || left.values.studentCode.localeCompare(right.values.studentCode, undefined, { numeric: true })),
    classrooms: references.classrooms.toSorted((left, right) => left.values.classroomCode.localeCompare(right.values.classroomCode, undefined, { numeric: true })),
    courses: references.courses.toSorted((left, right) => left.values.courseCode.localeCompare(right.values.courseCode, undefined, { numeric: true })),
    timetable: references.timetable.toSorted((left, right) => Number(left.values.yearLevel) - Number(right.values.yearLevel) || left.values.timetableCode.localeCompare(right.values.timetableCode, undefined, { numeric: true })),
    attendance: references.attendance.toSorted((left, right) => (studentYears.get(left.values.studentId) ?? 99) - (studentYears.get(right.values.studentId) ?? 99) || left.values.attendanceCode.localeCompare(right.values.attendanceCode, undefined, { numeric: true })),
  };
}
