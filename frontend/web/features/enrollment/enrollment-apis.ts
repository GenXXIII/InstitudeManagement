import { classroomAssignmentApi } from "./classrooms/classroom-assignment-api";
import type { EnrollmentResource } from "./common/enrollment-types";
import type { EnrollmentResourceClient } from "./common/enrollment-resource-client";
import { courseAssignmentApi } from "./courses/course-assignment-api";
import { departmentEnrollmentApi } from "./departments/department-enrollment-api";
import { studentEnrollmentApi } from "./students/student-enrollment-api";
import { teacherAssignmentApi } from "./teachers/teacher-assignment-api";
import { timetableEnrollmentApi } from "./timetable/timetable-enrollment-api";

const enrollmentApis = {
  students: studentEnrollmentApi,
  teachers: teacherAssignmentApi,
  courses: courseAssignmentApi,
  classrooms: classroomAssignmentApi,
  timetable: timetableEnrollmentApi,
  departments: departmentEnrollmentApi,
} satisfies Record<Exclude<EnrollmentResource, "student-assignments">, EnrollmentResourceClient>;

export function enrollmentApiFor(resource: EnrollmentResource): EnrollmentResourceClient {
  return resource === "student-assignments" ? studentEnrollmentApi : enrollmentApis[resource];
}
