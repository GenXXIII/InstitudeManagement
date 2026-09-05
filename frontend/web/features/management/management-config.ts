import { classroomDefaults, classroomFields } from "./classrooms/classroom-config";
import { courseDefaults, courseFields } from "./courses/course-config";
import { departmentDefaults, departmentFields } from "./departments/department-config";
import type { Field, ManagementModule, References } from "./management-types";
import { studentDefaults, studentFields } from "./students/student-config";
import { teacherDefaults, teacherFields } from "./teachers/teacher-config";
import { timetableDefaults, timetableFields } from "@/features/timetable/timetable-config";

export const emptyReferences: References = { departments: [], teachers: [], students: [], classrooms: [], courses: [], timetable: [], attendance: [] };
export const managementCopy: Record<ManagementModule, { title: string; description: string; singular: string }> = {
  overview: { title: "Management Overview", description: "Review current coded institute data through connected department workspaces.", singular: "item" },
  students: { title: "Student profile management", description: "Maintain each student's identity, profile photo, name, email, and personal record. Academic placement is shown under Academic enrollment.", singular: "student" },
  teachers: { title: "Teacher profile management", description: "Maintain each teacher's identity, profile photo, name, email, and personal record. Academic assignments are managed under Academic enrollment.", singular: "teacher" },
  classrooms: { title: "Learning-space management", description: "Manage institute-shared classroom and meeting-room information, including the status that controls timetable availability.", singular: "learning space" },
  courses: { title: "Course master management", description: "Maintain course identity and name only. Department, teacher, year, and capacity belong to Academic enrollment.", singular: "course" },
  timetable: { title: "Schedule management", description: "View and manage each schedule by code, time, day, and creation date.", singular: "schedule" },
  departments: { title: "Department management", description: "Organize academic units and appoint an existing teacher as each department head.", singular: "department" },
};

export const managementFields: Record<Exclude<ManagementModule, "overview">, Field[]> = {
  students: studentFields,
  teachers: teacherFields,
  classrooms: classroomFields,
  courses: courseFields,
  timetable: timetableFields,
  departments: departmentFields,
};

export function moduleDefaults(module: Exclude<ManagementModule, "overview">, departmentId: string): Record<string, string> {
  if (module === "students") return studentDefaults(departmentId);
  if (module === "teachers") return teacherDefaults();
  if (module === "classrooms") return classroomDefaults();
  if (module === "courses") return courseDefaults(departmentId);
  if (module === "timetable") return timetableDefaults(departmentId);
  return departmentDefaults();
}
