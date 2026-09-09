import { departmentApi } from "@/features/management/departments/department-api";
import type { DepartmentItem } from "@/features/management/departments/department-types";
import { workflowCode } from "@/lib/workflow-code";
import type { EnrollmentResourceClient } from "./common/enrollment-resource-client";
import type { EnrollmentItem } from "./common/enrollment-types";
import { studentEnrollmentApi } from "./students/student-enrollment-api";
import { timetableEnrollmentApi } from "./timetable/timetable-enrollment-api";

export type AssignmentProjectionResource = "student-assignments" | "teachers" | "courses" | "classrooms" | "departments";

export type AssignmentProjection = Record<AssignmentProjectionResource, EnrollmentItem[]>;

export function assignmentProjectionClient(resource: AssignmentProjectionResource): EnrollmentResourceClient {
  return {
    get: async (search = "", departmentId = "", year = "") => {
      const [students, timetable, departments] = await Promise.all([
        studentEnrollmentApi.get("", departmentId, year),
        timetableEnrollmentApi.get("", departmentId, year),
        resource === "departments" ? departmentApi.get() : Promise.resolve([]),
      ]);
      return filterProjection(deriveAssignmentProjection(students, timetable, departments)[resource], search);
    },
    update: async () => { throw new Error("Assignment views are generated from Student Enrollment and Timetable Enrollment."); },
    remove: async () => { throw new Error("Assignment views are generated from Student Enrollment and Timetable Enrollment."); },
  };
}

export function deriveAssignmentProjection(
  studentRows: EnrollmentItem[],
  timetableRows: EnrollmentItem[],
  departments: DepartmentItem[],
): AssignmentProjection {
  const students = studentRows.filter(item => item.values.status === "Active" && cohortKey(item, "year"));
  const timetable = timetableRows.filter(item => item.values.status === "Active" && cohortKey(item, "yearLevel"));
  const studentCohorts = new Set(students.map(item => cohortKey(item, "year")!));
  const timetableCohorts = new Set(timetable.map(item => cohortKey(item, "yearLevel")!));
  const matchedStudents = students.filter(item => timetableCohorts.has(cohortKey(item, "year")!));
  const matchedTimetable = timetable.filter(item => studentCohorts.has(cohortKey(item, "yearLevel")!));
  const timetableByCohort = groupBy(matchedTimetable, item => cohortKey(item, "yearLevel")!);

  const studentAssignments = matchedStudents.map(student => {
    const schedules = timetableByCohort.get(cohortKey(student, "year")!) ?? [];
    return {
      ...student,
      values: {
        ...student.values,
        courses: uniqueValues(schedules, "course").join(", "),
        teachers: uniqueValues(schedules, "teacher").join(", "),
        classrooms: uniqueValues(schedules, "classroom").join(", "),
        timetableCount: schedules.length.toString(),
      },
    };
  });

  const teacherAssignments = [...groupBy(matchedTimetable.filter(item => Boolean(item.values.teacherId)), item => periodGroupKey(item, item.values.teacherId)).values()]
    .map(schedules => {
      const first = schedules[0];
      return item(periodRowId(first, first.values.teacherId), {
        enrollmentCode: workflowCode(first.values.teacherCode, "teacher", "enrollment"),
        teacherCode: first.values.teacherCode,
        name: first.values.teacher,
        departmentId: singleValue(schedules, "departmentId"),
        department: uniqueValues(schedules, "department").join(", "),
        status: "Assigned",
        courseCount: uniqueValues(schedules, "courseId").length.toString(),
        courses: uniqueValues(schedules, "course").join(", "),
        yearLevels: uniqueValues(schedules, "yearLevel").map(value => `Year ${value}`).join(", "),
        academicYear: first.values.academicYear,
        semester: first.values.semester,
        periodState: first.values.periodState || "Current",
        createAt: earliestDate(schedules),
      });
    });

  const courseAssignments = [...groupBy(matchedTimetable.filter(item => Boolean(item.values.courseId)), item => periodGroupKey(item, item.values.courseId)).values()]
    .map(schedules => {
      const first = schedules[0];
      return item(periodRowId(first, first.values.courseId), {
        enrollmentCode: workflowCode(first.values.courseCode, "course", "enrollment"),
        courseCode: first.values.courseCode,
        name: first.values.course,
        departmentId: singleValue(schedules, "departmentId"),
        department: uniqueValues(schedules, "department").join(", "),
        teacherId: singleValue(schedules, "teacherId"),
        teacher: uniqueValues(schedules, "teacher").join(", "),
        year: singleValue(schedules, "yearLevel"),
        status: "Active",
        academicYear: first.values.academicYear,
        semester: first.values.semester,
        periodState: first.values.periodState || "Current",
        createAt: earliestDate(schedules),
      });
    });

  const classroomAssignments = [...groupBy(matchedTimetable.filter(item => Boolean(item.values.classroomId)), item => periodGroupKey(item, item.values.classroomId)).values()]
    .map(schedules => {
      const first = schedules[0];
      return item(periodRowId(first, first.values.classroomId), {
        enrollmentCode: workflowCode(first.values.classroom, "classroom", "enrollment"),
        classroomCode: first.values.classroom,
        building: first.values.building || first.values.classroom,
        roomType: first.values.roomType || first.values.classroomType || "Classroom",
        departmentId: singleValue(schedules, "departmentId"),
        department: uniqueValues(schedules, "department").join(", "),
        access: uniqueValues(schedules, "departmentId").length === 1 ? "Department only" : "Shared institute",
        capacity: first.values.capacity,
        status: first.values.classroomStatus || "Maintenance",
        courses: uniqueValues(schedules, "course").join(", "),
        teachers: uniqueValues(schedules, "teacher").join(", "),
        yearLevels: uniqueValues(schedules, "yearLevel").map(value => `Year ${value}`).join(", "),
        academicYear: first.values.academicYear,
        semester: first.values.semester,
        periodState: first.values.periodState || "Current",
        createAt: earliestDate(schedules),
      });
    });

  const departmentById = new Map(departments.map(department => [department.id, department]));
  const departmentAssignments = [...groupBy(matchedTimetable.filter(item => Boolean(item.values.departmentId)), item => periodGroupKey(item, item.values.departmentId)).values()]
    .map(schedules => {
      const first = schedules[0];
      const id = first.values.departmentId;
      const source = departmentById.get(id);
      const departmentStudents = matchedStudents.filter(student => student.values.departmentId === id && student.values.academicYear === first.values.academicYear && student.values.semester === first.values.semester);
      return item(periodRowId(first, id), {
        departmentCode: source?.values.departmentCode ?? "",
        name: source?.values.name ?? first.values.department,
        studentCount: departmentStudents.length.toString(),
        timetableCount: schedules.length.toString(),
        yearLevels: uniqueValues(schedules, "yearLevel").map(value => `Year ${value}`).join(", "),
        status: "Active",
        academicYear: first.values.academicYear,
        semester: first.values.semester,
        periodState: first.values.periodState || "Current",
        createAt: source?.values.createAt ?? earliestDate(schedules),
      });
    });

  return {
    "student-assignments": studentAssignments,
    teachers: teacherAssignments,
    courses: courseAssignments,
    classrooms: classroomAssignments,
    departments: departmentAssignments,
  };
}

function cohortKey(item: EnrollmentItem, yearKey: "year" | "yearLevel") {
  const values = item.values;
  if (!values.departmentId || !values[yearKey] || !values.shift || !values.academicYear || !values.semester) return "";
  return [values.departmentId, values[yearKey], values.shift, values.academicYear, values.semester]
    .map(value => value.trim().toUpperCase())
    .join(":");
}

function periodGroupKey(item: EnrollmentItem, resourceId: string) {
  return [resourceId, item.values.academicYear, item.values.semester].join("|");
}

function periodRowId(item: EnrollmentItem, resourceId: string) {
  return `${resourceId}-${item.values.academicYear}-${item.values.semester}`;
}

function groupBy(items: EnrollmentItem[], key: (item: EnrollmentItem) => string) {
  const groups = new Map<string, EnrollmentItem[]>();
  for (const entry of items) {
    const value = key(entry);
    groups.set(value, [...(groups.get(value) ?? []), entry]);
  }
  return groups;
}

function uniqueValues(items: EnrollmentItem[], key: string) {
  return [...new Set(items.map(item => item.values[key]).filter(Boolean))]
    .toSorted((left, right) => left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }));
}

function singleValue(items: EnrollmentItem[], key: string) {
  const values = uniqueValues(items, key);
  return values.length === 1 ? values[0] : "";
}

function earliestDate(items: EnrollmentItem[]) {
  return uniqueValues(items, "createAt").toSorted()[0] ?? "";
}

function item(id: string, values: Record<string, string>): EnrollmentItem {
  return { id, values };
}

function filterProjection(items: EnrollmentItem[], search: string) {
  const query = search.trim().toLocaleLowerCase();
  if (!query) return items;
  return items.filter(item =>
    item.id.toLocaleLowerCase().includes(query)
    || Object.values(item.values).some(value => value.toLocaleLowerCase().includes(query)));
}
