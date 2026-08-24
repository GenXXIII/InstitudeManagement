import { request } from "@/lib/http";
import type { ClassSessionAttendanceUpdate, OperationalRecord } from "./record-types";

export const recordApi = {
  get: (module: string, search = "", departmentId = "", history = false) => request<OperationalRecord[]>(`/api/operational-records/${module}?search=${encodeURIComponent(search)}${departmentId ? `&departmentId=${encodeURIComponent(departmentId)}` : ""}${history ? "&history=true" : ""}`),
  updateSession: (id: string, students: ClassSessionAttendanceUpdate[]) => request<void>(`/api/operational-records/sessions/${id}`, { method: "PUT", body: JSON.stringify({ students }) }),
  updateGrade: (studentId: string, courseId: string, score: number) => request<void>("/api/grades", { method: "POST", body: JSON.stringify({ studentId, courseId, score }) }),
  async updateStudentAttendance(sessionId: string, studentId: string, status: string, checkedInAt: string) {
    const sessions = await recordApi.get("sessions");
    const session = sessions.find(item => item.id === sessionId);
    if (!session) throw new Error("The current-semester class session could not be found.");
    const students = session.activities.filter(activity => activity.Activity === "Student attendance").map(activity => ({
      studentId: activity.StudentId,
      status: activity.StudentId === studentId ? status : activity.Attendance,
      checkedInAt: activity.StudentId === studentId ? checkedInAt : activity["Check in"] === "No check-in" ? "" : activity["Check in"],
    }));
    await recordApi.updateSession(sessionId, students);
  },
};
