import type { Field } from "../management-types";

export const studentFields: Field[] = [
  { key: "photoDataUrl", label: "4×6 student photo", type: "photo", required: true },
  { key: "number", label: "Student ID", required: true },
  { key: "name", label: "Full name", required: true },
  { key: "email", label: "Email", type: "email", required: true },
  { key: "departmentId", label: "Department", type: "select", source: "departments", required: true },
  { key: "year", label: "Year level", type: "number", required: true },
  { key: "status", label: "Status", type: "select", options: ["Active", "Inactive"], required: true },
];

export const studentDefaults = (departmentId: string) => ({ departmentId, year: "1", status: "Active" });
