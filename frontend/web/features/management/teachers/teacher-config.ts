import type { Field } from "../management-types";

export const teacherFields: Field[] = [
  { key: "photoDataUrl", label: "4×6 teacher photo", type: "photo", required: true },
  { key: "teacherCode", label: "TeacherCode", required: true },
  { key: "name", label: "Full name", required: true },
  { key: "email", label: "Email", type: "email", required: true },
];

export const teacherDefaults = () => ({ status: "Available" });
