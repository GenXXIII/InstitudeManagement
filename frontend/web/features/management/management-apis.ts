import { attendanceApi } from "./attendance/attendance-api";
import { classroomApi } from "./classrooms/classroom-api";
import { courseApi } from "./courses/course-api";
import { departmentApi } from "./departments/department-api";
import { gradeApi } from "./grades/grade-api";
import { studentApi } from "./students/student-api";
import { teacherApi } from "./teachers/teacher-api";
import { timetableApi } from "./timetable/timetable-api";

export const managementApis = {
  students: studentApi,
  teachers: teacherApi,
  classrooms: classroomApi,
  courses: courseApi,
  timetable: timetableApi,
  attendance: attendanceApi,
  departments: departmentApi,
  grades: gradeApi,
};
