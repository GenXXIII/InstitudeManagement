import type { Field } from "../management-types";

export const courseFields: Field[] = [
  { key: "code", label: "Course code", required: true },
  { key: "name", label: "Course name", required: true },
  { key: "departmentId", label: "Department", type: "select", source: "departments", required: true },
  { key: "teacherId", label: "Assigned teacher", type: "select", source: "teachers", required: true },
  { key: "credits", label: "Credits", type: "number", required: true },
  { key: "capacity", label: "Student capacity", type: "number", required: true },
  { key: "status", label: "Status", type: "select", options: ["Active", "Inactive"], required: true },
];

export const courseDefaults = (departmentId: string) => ({ departmentId, credits: "3", capacity: "40", status: "Active" });
