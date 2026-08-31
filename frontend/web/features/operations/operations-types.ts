import type { Activity, Metric } from "@/lib/types/presentation-types";

export type OperationSummary = { module: string; summary: string; value: string; detail: string; status: string; route: string; tone: string };
export type StudentOperation = { id: string; student: string; studentCode: string; department: string; year: number; shift: string; attendanceStatus: string };
export type TeacherOperation = { id: string; teacher: string; teacherCode: string; department: string; status: string };
export type ClassroomOperation = { id: string; room: string; roomType: string; floor: number; building: string; capacity: number; device: string; status: string; course: string; teacher: string; teacherAttendance: string; statusDetail: string };
export type CourseOperation = { id: string; course: string; courseCode: string; teacher: string; department: string; capacity: number; status: string; teacherAttendance: string; statusDetail: string };
export type WeeklyTimetableSlot = { id: string; timetableCode: string; day: string; session: string; startsAt: string; endsAt: string; course: string; teacher: string; yearLevel: number; room: string; roomType: string; status: string; teacherAttendance: string; statusDetail: string };
export type TimetablePeriod = { dayGroup: "Weekday" | "Weekend"; session: "Morning" | "Afternoon" | "Evening"; startsAt: string; endsAt: string };
export type TimetableRoom = { id: string; room: string; roomType: string; status: string };
export type AttendanceOperation = { id: string; time: string; student: string; studentCode: string; method: string; status: string };
export type DepartmentOperation = { id: string; department: string; head: string; students: number; teachers: number; courses: number; status: string };
export type GradeOperation = { id: string; student: string; course: string; score: number; grade: string; term: string };

export type Operation = {
  module: string;
  title: string;
  description: string;
  metrics: Metric[];
  activity: Activity[];
  attention: Activity[];
  summary?: OperationSummary[];
  students?: StudentOperation[];
  teachers?: TeacherOperation[];
  classrooms?: ClassroomOperation[];
  courses?: CourseOperation[];
  weeklySchedule?: WeeklyTimetableSlot[];
  timetablePeriods?: TimetablePeriod[];
  timetableRooms?: TimetableRoom[];
  attendance?: AttendanceOperation[];
  departments?: DepartmentOperation[];
  grades?: GradeOperation[];
};
