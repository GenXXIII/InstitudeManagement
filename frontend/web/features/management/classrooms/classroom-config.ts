import type { Field } from "../management-types";

export const classroomFields: Field[] = [
  { key: "classroomCode", label: "ClassroomCode", required: true },
  { key: "building", label: "Building", required: true },
  { key: "roomType", label: "Learning-space type", type: "select", options: ["Classroom", "Meeting Room"], required: true },
  { key: "capacity", label: "Capacity", type: "number", required: true },
  { key: "status", label: "Status", type: "select", options: ["Available", "Maintenance"], required: true },
];

export const classroomDefaults = () => ({ roomType: "Classroom", capacity: "40", status: "Available", deviceOnline: "true" });
