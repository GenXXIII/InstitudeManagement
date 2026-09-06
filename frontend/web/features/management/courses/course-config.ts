import type { Field } from "../management-types";

export const courseFields: Field[] = [
  { key: "courseCode", label: "CourseCode", required: true },
  { key: "name", label: "Course name", required: true },
];

export const courseDefaults = (departmentId: string) => { void departmentId; return { status: "Active" }; };
