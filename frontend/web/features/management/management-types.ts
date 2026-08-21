import type { AttendanceItem } from "./types/attendance";
import type { ClassroomItem } from "./types/classroom";
import type { CourseItem } from "./types/course";
import type { DepartmentItem } from "./types/department";
import type { GradeItem } from "./types/grade";
import type { StudentItem } from "./types/student";
import type { TeacherItem } from "./types/teacher";
import type { TimetableItem } from "./types/timetable";

export type ManagementModule = "overview" | "students" | "teachers" | "classrooms" | "courses" | "timetable" | "attendance" | "departments" | "grades";
export type ManagementResource = Exclude<ManagementModule, "overview">;
export type ManagementItem = StudentItem | TeacherItem | ClassroomItem | CourseItem | TimetableItem | AttendanceItem | DepartmentItem | GradeItem;
export type ManagementItemMap = {
  students: StudentItem;
  teachers: TeacherItem;
  classrooms: ClassroomItem;
  courses: CourseItem;
  timetable: TimetableItem;
  attendance: AttendanceItem;
  departments: DepartmentItem;
  grades: GradeItem;
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
export type Field = { key: string; label: string; type?: "text" | "email" | "number" | "select" | "photo" | "date" | "time" | "checkbox"; source?: keyof References; options?: string[]; required?: boolean };
