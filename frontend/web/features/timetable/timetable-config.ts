import type { Field } from "@/features/management/management-types";

export const timetableFields: Field[] = [
  { key: "timetableCode", label: "Code", required: true },
  { key: "dayOfWeek", label: "Day", type: "select", options: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"], required: true },
  { key: "shift", label: "Shift", type: "select", options: ["Morning", "Afternoon", "Evening", "Weekend"], required: true },
  { key: "period", label: "Teaching period", type: "select", required: true },
];

export const timetableDefaults = (departmentId: string) => ({ departmentId, dayOfWeek: "Monday", shift: "Morning", period: "07:30|09:00", startsAt: "07:30", endsAt: "09:00", status: "Upcoming" });
