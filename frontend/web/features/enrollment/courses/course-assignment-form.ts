import type { DepartmentItem } from "@/features/management/departments/department-types";
import { yearOptions, type EnrollmentField } from "../common/enrollment-field";
import type { EnrollmentItem } from "../common/enrollment-types";

export function courseAssignmentFields(
  departments: DepartmentItem[],
  availableTeachers: EnrollmentItem[],
  teacherRequired: boolean,
): EnrollmentField[] {
  return [
    { key: "departmentId", label: "Department", type: "select", options: departments.map(department => ({ id: department.id, label: department.values.name })), required: true },
    { key: "teacherId", label: "Assigned teacher", type: "select", options: [...(teacherRequired ? [] : [{ id: "", label: "Assign later" }]), ...availableTeachers.map(teacher => ({ id: teacher.id, label: `${teacher.values.teacherCode} - ${teacher.values.name}` }))], required: teacherRequired },
    { key: "year", label: "Year level", type: "select", options: yearOptions(), required: true },
    { key: "capacity", label: "Student capacity", type: "number", required: true },
    { key: "status", label: "Assignment status", type: "select", options: ["Active", "Paused"].map(id => ({ id, label: id })), required: true },
  ];
}

export function courseAssignmentDefaults(departmentId: string, year: string, capacity: string): Record<string, string> {
  return { departmentId, teacherId: "", year: year || "1", capacity: capacity || "40", status: "Active" };
}
