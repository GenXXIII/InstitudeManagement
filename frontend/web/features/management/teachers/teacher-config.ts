import type { Field } from "../management-types";

export const teacherFields: Field[] = [
  { key: "photoDataUrl", label: "4×6 teacher photo", type: "photo", required: true },
  { key: "number", label: "Teacher ID", required: true },
  { key: "name", label: "Full name", required: true },
  { key: "email", label: "Email", type: "email", required: true },
  { key: "departmentId", label: "Department", type: "select", source: "departments", required: true },
  { key: "status", label: "Work status", type: "select", options: ["Available", "Teaching", "Meeting", "On leave", "Inactive"], required: true },
];

export const teacherDefaults = (departmentId: string) => ({ departmentId, status: "Available" });
