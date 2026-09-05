import type { SearchableOption } from "@/components/searchable-select";
import type { DepartmentItem } from "@/features/management/departments/department-types";
import { classroomAssignmentDefaults, classroomAssignmentFields } from "./classrooms/classroom-assignment-form";
import type { EnrollmentField } from "./common/enrollment-field";
import type { EnrollmentItem, EnrollmentResource } from "./common/enrollment-types";
import { courseAssignmentDefaults, courseAssignmentFields } from "./courses/course-assignment-form";
import { studentEnrollmentDefaults, studentEnrollmentFields } from "./students/student-enrollment-form";
import { teacherAssignmentDefaults, teacherAssignmentFields } from "./teachers/teacher-assignment-form";
import { timetableEnrollmentDefaults } from "./timetable/timetable-enrollment-form";
import type { WorkflowCodeResource } from "@/lib/workflow-code";

export type { EnrollmentField } from "./common/enrollment-field";

export function buildEnrollmentFields({ resource, departments, availableTeachers, teacherRequired }: {
  resource: EnrollmentResource;
  departments: DepartmentItem[];
  availableTeachers: EnrollmentItem[];
  teacherRequired: boolean;
}): EnrollmentField[] {
  const code: EnrollmentField = { key: "enrollmentCode", label: "EnrollmentCode", type: "text", required: true };
  if (resource === "students") return [code, ...studentEnrollmentFields(departments)];
  if (resource === "teachers") return [code, ...teacherAssignmentFields(departments)];
  if (resource === "courses") return [code, ...courseAssignmentFields(departments, availableTeachers, teacherRequired)];
  if (resource === "classrooms") return [code, ...classroomAssignmentFields(departments)];
  if (resource === "timetable") return [code];
  return [];
}

export function enrollmentCodeResource(resource: EnrollmentResource): WorkflowCodeResource | undefined {
  if (resource === "students") return "student";
  if (resource === "teachers") return "teacher";
  if (resource === "courses") return "course";
  if (resource === "classrooms") return "classroom";
  if (resource === "timetable") return "timetable";
  return undefined;
}

export function enrollmentDefaults(
  resource: EnrollmentResource,
  departmentId: string,
  year: string,
  courseCapacity = "40",
): Record<string, string> {
  if (resource === "students") return studentEnrollmentDefaults(departmentId, year);
  if (resource === "teachers") return teacherAssignmentDefaults(departmentId);
  if (resource === "courses") return courseAssignmentDefaults(departmentId, year, courseCapacity);
  if (resource === "classrooms") return classroomAssignmentDefaults(departmentId);
  if (resource === "timetable") return timetableEnrollmentDefaults(year);
  return {};
}

export function candidateName(resource: EnrollmentResource) {
  if (resource === "students") return "Student profile";
  if (resource === "timetable") return "Timetable code";
  if (resource === "teachers") return "Teacher profile";
  if (resource === "courses") return "Course record";
  if (resource === "classrooms") return "Learning space";
  return "Enrollment record";
}

export function candidateOption(item: EnrollmentItem): SearchableOption {
  const values = item.values;
  if (values.timetableCode) {
    return {
      id: item.id,
      label: [values.timetableCode, values.courseCode, values.teacherCode].filter(Boolean).join(" - "),
      detail: [values.enrollmentStatus, values.course, values.teacher, values.dayOfWeek, `${values.startsAt}-${values.endsAt}`, values.classroom].filter(Boolean).join(" - "),
    };
  }
  const code = values.studentCode || values.teacherCode || values.courseCode || values.classroomCode;
  const name = values.name || values.course || [values.building, values.roomType].filter(Boolean).join(" - ");
  return { id: item.id, label: [code, name].filter(Boolean).join(" - "), detail: values.email || undefined };
}

export function relationshipOptions(items: EnrollmentItem[], idKey: string, codeKey: string, nameKey: string): SearchableOption[] {
  const options = new Map<string, SearchableOption>();
  for (const item of items) {
    const id = item.values[idKey];
    if (id && !options.has(id)) options.set(id, { id, label: [item.values[codeKey], item.values[nameKey]].filter(Boolean).join(" - ") });
  }
  return [...options.values()].toSorted((left, right) => left.label.localeCompare(right.label, undefined, { numeric: true, sensitivity: "base" }));
}
