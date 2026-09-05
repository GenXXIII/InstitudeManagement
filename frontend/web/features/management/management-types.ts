import type { AttendanceItem } from "@/features/attendance/attendance-types";
import type { ClassroomItem } from "@/features/management/classrooms/classroom-types";
import type { CourseItem } from "@/features/management/courses/course-types";
import type { DepartmentItem } from "@/features/management/departments/department-types";
import type { StudentItem } from "@/features/management/students/student-types";
import type { TeacherItem } from "@/features/management/teachers/teacher-types";
import type { TimetableItem } from "@/features/timetable/timetable-types";

export type ManagementModule = "overview" | "students" | "teachers" | "classrooms" | "courses" | "timetable" | "departments";
export type ManagementResource = Exclude<ManagementModule, "overview">;
export type ManagementItem = StudentItem | TeacherItem | ClassroomItem | CourseItem | TimetableItem | DepartmentItem;
export type ManagementItemMap = {
  students: StudentItem;
  teachers: TeacherItem;
  classrooms: ClassroomItem;
  courses: CourseItem;
  timetable: TimetableItem;
  departments: DepartmentItem;
};
export type References = {
  departments: DepartmentItem[];
  teachers: TeacherItem[];
  students: StudentItem[];
  classrooms: ClassroomItem[];
  courses: CourseItem[];
  timetable: TimetableItem[];
  attendance: AttendanceItem[];
};
export type RelationshipResource = Exclude<keyof References, "attendance">;
export type Field = { key: string; label: string; type?: "text" | "email" | "number" | "select" | "photo" | "date" | "time" | "checkbox"; source?: RelationshipResource; options?: string[]; required?: boolean; readOnly?: boolean };
