import type { RelationshipResource } from "./management-types";

type RelationshipCreateTarget = {
  label: string;
  path: string;
};

const relationshipCreateTargets: Partial<Record<RelationshipResource, RelationshipCreateTarget>> = {
  departments: { label: "Create department data", path: "/management/departments" },
  teachers: { label: "Create teacher data", path: "/management/teachers" },
  students: { label: "Create student data", path: "/management/students" },
  classrooms: { label: "Create classroom data", path: "/management/classrooms" },
  courses: { label: "Create course data", path: "/management/courses" },
};

export function relationshipCreateTarget(source: RelationshipResource | undefined) {
  return source ? relationshipCreateTargets[source] : undefined;
}
