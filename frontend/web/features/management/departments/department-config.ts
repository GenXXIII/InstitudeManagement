import type { Field } from "../management-types";

export const departmentFields: Field[] = [
  { key: "departmentCode", label: "DepartmentCode", required: true },
  { key: "name", label: "Department name", required: true },
  { key: "headTeacherId", label: "Head of department", type: "select", source: "teachers", required: true },
];

export const departmentDefaults = () => ({ status: "Active" });
