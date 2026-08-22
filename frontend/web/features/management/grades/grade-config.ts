import type { Field } from "../management-types";

export const gradeFields: Field[] = [
  { key: "gradeCode", label: "GradeCode", required: true },
  { key: "studentId", label: "Student", type: "select", source: "students", required: true },
  { key: "courseId", label: "Course", type: "select", source: "courses", required: true },
  { key: "score", label: "Score", type: "number", required: true },
];

export const gradeDefaults = (departmentId: string) => ({ departmentId, term: "Semester 1", score: "" });
