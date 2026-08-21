import type { Field } from "../management-types";

export const timetableFields: Field[] = [
  { key: "courseId", label: "Course", type: "select", source: "courses", required: true },
  { key: "teacherId", label: "Teacher", type: "select", source: "teachers", required: true },
  { key: "classroomId", label: "Classroom or meeting room", type: "select", source: "classrooms", required: true },
  { key: "yearLevel", label: "Student year", type: "select", options: ["1", "2", "3", "4"], required: true },
  { key: "dayOfWeek", label: "Day", type: "select", options: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"], required: true },
  { key: "period", label: "Teaching period", type: "select", required: true },
  { key: "status", label: "Status", type: "select", options: ["Upcoming", "Running", "Completed", "Cancelled"], required: true },
];

export const timetableDefaults = (departmentId: string) => ({ departmentId, yearLevel: "1", dayOfWeek: "Monday", period: "07:30|09:00", startsAt: "07:30", endsAt: "09:00", status: "Upcoming" });
