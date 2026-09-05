import type { DepartmentItem } from "@/features/management/departments/department-types";
import { yearOptions, type EnrollmentField } from "../common/enrollment-field";

export function studentEnrollmentFields(departments: DepartmentItem[]): EnrollmentField[] {
  return [
    { key: "departmentId", label: "Department selected for Year 2-4", type: "select", options: departments.map(department => ({ id: department.id, label: department.values.name })), required: true },
    { key: "year", label: "Year level", type: "select", options: yearOptions(), required: true },
    { key: "shift", label: "Learning shift", type: "select", options: ["Morning", "Afternoon", "Evening", "Weekend"].map(id => ({ id, label: id })), required: true },
  ];
}

export function studentEnrollmentDefaults(departmentId: string, year: string): Record<string, string> {
  return { departmentId, year: year || "1", shift: "Morning", status: "Active" };
}
