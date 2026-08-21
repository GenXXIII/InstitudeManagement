import type { Field } from "../management-types";

export const classroomFields: Field[] = [
  { key: "code", label: "Room code", required: true },
  { key: "building", label: "Building", required: true },
  { key: "roomType", label: "Learning-space type", type: "select", options: ["Classroom", "Meeting Room"], required: true },
  { key: "departmentId", label: "Department", type: "select", source: "departments", required: true },
  { key: "capacity", label: "Capacity", type: "number", required: true },
  { key: "status", label: "Operational status", type: "select", options: ["Available", "Starting", "Offline", "Inactive"], required: true },
  { key: "deviceOnline", label: "Attendance device online", type: "checkbox" },
];

export const classroomDefaults = (departmentId: string) => ({ departmentId, roomType: "Classroom", capacity: "40", status: "Available", deviceOnline: "true" });
