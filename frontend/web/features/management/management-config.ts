import { attendanceDefaults, attendanceFields } from "./attendance/attendance-config";
import { classroomDefaults, classroomFields } from "./classrooms/classroom-config";
import { courseDefaults, courseFields } from "./courses/course-config";
import { departmentDefaults, departmentFields } from "./departments/department-config";
import { gradeDefaults, gradeFields } from "./grades/grade-config";
import type { Field, ManagementModule, References } from "./management-types";
import { studentDefaults, studentFields } from "./students/student-config";
import { teacherDefaults, teacherFields } from "./teachers/teacher-config";
import { timetableDefaults, timetableFields } from "./timetable/timetable-config";

export const emptyReferences: References = { departments: [], teachers: [], students: [], classrooms: [], courses: [], timetable: [], attendance: [] };
export const managementCopy: Record<ManagementModule, { title: string; description: string; singular: string }> = {
  overview: { title: "Institute management", description: "Manage current institute data through connected department workspaces.", singular: "item" },
  students: { title: "Student management", description: "Enroll students, maintain 4×6 profile photos, and assign each learner to a department.", singular: "student" },
  teachers: { title: "Teacher management", description: "Maintain faculty profiles, availability, and leadership eligibility.", singular: "teacher" },
  classrooms: { title: "Learning-space management", description: "Manage classrooms and meeting rooms with capacity, operational state, and live study status.", singular: "learning space" },
  courses: { title: "Course management", description: "Connect courses to departments and eligible teachers with capacity rules.", singular: "course" },
  timetable: { title: "Timetable management", description: "Manage every scheduled class as readable data rows with day, time, course, cohort, teacher, room, student count, status, and actions.", singular: "class" },
  attendance: { title: "Attendance management", description: "Record and correct current attendance while preserving each change in immutable history.", singular: "attendance entry" },
  departments: { title: "Department management", description: "Organize academic units and appoint an existing teacher as each department head.", singular: "department" },
  grades: { title: "Grade management", description: "Maintain department gradebooks by connecting students to courses and terms.", singular: "grade" },
};

export const managementFields: Record<Exclude<ManagementModule, "overview">, Field[]> = {
  students: studentFields,
  teachers: teacherFields,
  classrooms: classroomFields,
  courses: courseFields,
  timetable: timetableFields,
  attendance: attendanceFields,
  departments: departmentFields,
  grades: gradeFields,
};

export function moduleDefaults(module: Exclude<ManagementModule, "overview">, departmentId: string): Record<string, string> {
  if (module === "students") return studentDefaults(departmentId);
  if (module === "teachers") return teacherDefaults();
  if (module === "classrooms") return classroomDefaults(departmentId);
  if (module === "courses") return courseDefaults(departmentId);
  if (module === "timetable") return timetableDefaults(departmentId);
  if (module === "attendance") return attendanceDefaults(departmentId);
  if (module === "departments") return departmentDefaults();
  return gradeDefaults(departmentId);
}
