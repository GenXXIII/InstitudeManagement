import { assignmentProjectionClient, type AssignmentProjectionResource } from "./assignment-projection-api";
import type { EnrollmentResourceClient } from "./common/enrollment-resource-client";
import type { EnrollmentResource } from "./common/enrollment-types";
import { studentEnrollmentApi } from "./students/student-enrollment-api";
import { timetableEnrollmentApi } from "./timetable/timetable-enrollment-api";

const sourceEnrollmentApis = {
  students: studentEnrollmentApi,
  timetable: timetableEnrollmentApi,
} satisfies Record<"students" | "timetable", EnrollmentResourceClient>;

const assignmentProjectionApis = {
  "student-assignments": assignmentProjectionClient("student-assignments"),
  teachers: assignmentProjectionClient("teachers"),
  courses: assignmentProjectionClient("courses"),
  classrooms: assignmentProjectionClient("classrooms"),
  departments: assignmentProjectionClient("departments"),
} satisfies Record<AssignmentProjectionResource, EnrollmentResourceClient>;

export function enrollmentApiFor(resource: EnrollmentResource): EnrollmentResourceClient {
  return resource === "students" || resource === "timetable"
    ? sourceEnrollmentApis[resource]
    : assignmentProjectionApis[resource];
}
