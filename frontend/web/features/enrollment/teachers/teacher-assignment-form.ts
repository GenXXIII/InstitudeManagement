import type { DepartmentItem } from "@/features/management/departments/department-types";
import type { EnrollmentField } from "../common/enrollment-field";

export function teacherAssignmentFields(departments: DepartmentItem[]): EnrollmentField[] {
  const departmentOptions = departments.map(department => ({ id: department.id, label: department.values.name }));
  return [
    { key: "departmentId", label: "Assigned department", type: "select", options: [{ id: "", label: "Unassigned" }, ...departmentOptions] },
    { key: "status", label: "Assignment status", type: "select", options: ["Assigned", "On leave", "Unassigned"].map(id => ({ id, label: id })), required: true },
  ];
}

export function teacherAssignmentDefaults(departmentId: string): Record<string, string> {
  return { departmentId, status: departmentId ? "Assigned" : "Unassigned" };
}
