import type { Field } from "../management-types";

export const studentFields: Field[] = [
  { key: "photoDataUrl", label: "4×6 student photo", type: "photo", required: true },
  { key: "studentCode", label: "StudentCode", required: true },
  { key: "name", label: "Full name", required: true },
  { key: "email", label: "Email", type: "email", required: true },
];

export const studentDefaults = (departmentId: string) => { void departmentId; return { status: "Active" }; };
