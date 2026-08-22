import type { Field } from "../management-types";

export const attendanceFields: Field[] = [
  { key: "attendanceCode", label: "AttendanceCode", required: true },
  { key: "studentId", label: "Student", type: "select", source: "students", required: true },
  { key: "date", label: "Attendance date", type: "date", required: true },
  { key: "checkedInAt", label: "Check-in time", type: "time" },
  { key: "status", label: "Status", type: "select", options: ["Present", "Late", "Absent", "Excused"], required: true },
  { key: "method", label: "Method", type: "select", options: ["ID Card", "Manual", "QR Code", "Biometric"], required: true },
];

export const attendanceDefaults = (departmentId: string) => ({ departmentId, date: new Date().toISOString().slice(0, 10), status: "Present", method: "ID Card" });
