import type { Field } from "../management-types";

export const departmentFields: Field[] = [
  { key: "code", label: "Department code", required: true },
  { key: "name", label: "Department name", required: true },
  { key: "headTeacherId", label: "Head of department", type: "select", source: "teachers", required: true },
  { key: "status", label: "Status", type: "select", options: ["Active", "Inactive"], required: true },
];

export const departmentDefaults = () => ({ status: "Active" });
