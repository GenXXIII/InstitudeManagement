import type { Field } from "../management-types";

export const courseFields: Field[] = [
  { key: "courseCode", label: "CourseCode", required: true },
  { key: "name", label: "Course name", required: true },
  { key: "departmentId", label: "Department", type: "select", source: "departments", required: true },
  { key: "teacherId", label: "Assigned teacher", type: "select", source: "teachers", required: true },
  { key: "capacity", label: "Student capacity", type: "number", required: true },
];

export const courseDefaults = (departmentId: string) => ({ departmentId, capacity: "40", status: "Active" });
