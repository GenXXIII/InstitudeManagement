import type { DepartmentItem } from "@/features/management/departments/department-types";
import type { EnrollmentField } from "../common/enrollment-field";

export function classroomAssignmentFields(departments: DepartmentItem[]): EnrollmentField[] {
  const departmentOptions = departments.map(department => ({ id: department.id, label: department.values.name }));
  return [
    { key: "departmentId", label: "Department access", type: "select", options: [{ id: "", label: "Whole institute" }, ...departmentOptions] },
    { key: "access", label: "Access", type: "select", options: ["Shared institute", "Department only"].map(id => ({ id, label: id })), required: true },
    { key: "capacity", label: "Assigned seat capacity", type: "number", required: true },
    { key: "status", label: "Assignment status", type: "select", options: ["Available", "Maintenance"].map(id => ({ id, label: id })), required: true },
  ];
}

export function classroomAssignmentDefaults(departmentId: string): Record<string, string> {
  return { departmentId, access: departmentId ? "Department only" : "Shared institute", capacity: "", status: "Available" };
}
