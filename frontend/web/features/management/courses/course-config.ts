import type { Field } from "../management-types";

export const courseFields: Field[] = [
  { key: "courseCode", label: "CourseCode", required: true },
  { key: "name", label: "Course name", required: true },
  { key: "yearLevel", label: "Student year", type: "select", options: ["1", "2", "3", "4"], required: true },
  { key: "semester", label: "Semester", type: "select", options: ["Semester 1", "Semester 2"], required: true },
];

export const courseDefaults = (departmentId: string) => { void departmentId; return { yearLevel: "", semester: "", status: "Active" }; };
