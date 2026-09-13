import type { Field } from "@/features/management/management-types";

export const timetableFields: Field[] = [
  { key: "timetableCode", label: "Code", required: true },
  { key: "dayOfWeek", label: "Day", type: "select", options: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"], required: true },
  { key: "shift", label: "Shift", type: "select", options: ["Morning", "Afternoon", "Evening", "Weekend"], required: true },
  { key: "startsAt", label: "Starting time", type: "time", required: true },
  { key: "endsAt", label: "Ending time", type: "time", required: true },
];

export const timetableDefaults = (departmentId: string) => ({ departmentId, dayOfWeek: "", shift: "", startsAt: "", endsAt: "", status: "Upcoming" });
