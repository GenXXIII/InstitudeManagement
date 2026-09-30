import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";
import type { ClassSessionAttendanceUpdate, OperationalRecord } from "./record-types";

export const recordApi = {
  get: (module: string, search = "", departmentId = "", history = false, year = "", period = "") => requestAllPages<OperationalRecord>(`/api/operational-records/${module}`, recordParams(search, departmentId, history, year, period)),
  getPage: (module: string, search = "", departmentId = "", history = false, year = "", period = "", options: PageOptions = {}) => request<PagedResult<OperationalRecord>>(`/api/operational-records/${module}?${appendPage(recordParams(search, departmentId, history, year, period), options)}`),
  updateSession: (id: string, students: ClassSessionAttendanceUpdate[]) => request<void>(`/api/operational-records/sessions/${id}`, { method: "PUT", body: JSON.stringify({ students }) }),
  updateGrade: (studentId: string, courseId: string, scores: { assignmentScore: number; midtermScore: number; finalExamScore: number }) => request<void>("/api/grades", { method: "POST", body: JSON.stringify({ studentId, courseId, ...scores }) }),
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

function recordParams(search: string, departmentId: string, history: boolean, year = "", period = "") {
  const params = new URLSearchParams({ search, history: String(history) });
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  if (period && period !== "all") params.set("period", period);
  return params;
}
