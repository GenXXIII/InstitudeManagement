import Constants from 'expo-constants';
import { Platform } from 'react-native';
import type { MobileSession } from '@/features/auth/auth-context';
import type { Announcement, AttendanceItem, GradeItem, GradeWeights, PortalData, ScheduleItem, StudentItem, TeacherItem } from './portal-types';

const configuredApiUrl = process.env.EXPO_PUBLIC_API_URL?.trim().replace(/\/$/, '');

export function getApiBaseUrl() {
  if (configuredApiUrl) return configuredApiUrl;
  if (Platform.OS === 'web' && typeof window !== 'undefined') return `http://${window.location.hostname}:5080`;
  const expoHost = Constants.expoConfig?.hostUri?.split(':')[0];
  return expoHost ? `http://${expoHost}:5080` : 'http://localhost:5080';
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${getApiBaseUrl()}${path}`, {
      ...init,
      headers: { Accept: 'application/json', 'Content-Type': 'application/json', ...init?.headers },
    });
  } catch {
    throw new Error(`Cannot connect to INK API at ${getApiBaseUrl()}. Keep the iPhone and computer on the same Wi-Fi network.`);
  }
  if (!response.ok) {
    const body = await response.text();
    let message = body || `Request failed (${response.status}).`;
    try {
      const problem = JSON.parse(body) as { detail?: string; title?: string; errors?: Record<string, string[]> };
      message = Object.values(problem.errors ?? {}).flat().join(' ') || problem.detail || problem.title || message;
    } catch { /* The API did not return a problem-details body. */ }
    throw new Error(message);
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}

export async function loadPortalData(session: MobileSession): Promise<PortalData> {
  const roleResource = session.role === 'teacher' ? 'teachers' : 'students';
  const profiles = await request<TeacherItem[] | StudentItem[]>(`/api/catalog/${roleResource}?search=${encodeURIComponent(session.email)}`);
  const baseProfile = profiles.find(item => item.values.email.trim().toLowerCase() === session.email) ?? null;
  const [announcements, gradeSettings] = await Promise.all([
    request<Announcement[]>('/api/notification-center/alerts'),
    request<{ values: Record<string, string> }>('/api/settings/grade-rules'),
  ]);
  const gradeWeights: GradeWeights = {
    attendance: Number(gradeSettings.values.attendanceWeight || 10),
    assignment: Number(gradeSettings.values.assignmentWeight || 20),
    midterm: Number(gradeSettings.values.midtermWeight || 20),
    finalExam: Number(gradeSettings.values.finalExamWeight || 50),
  };
  if (!baseProfile) return { role: session.role, profile: null, schedule: [], students: [], attendance: [], grades: [], gradeWeights, announcements };

  const [schedule, roleEnrollments] = await Promise.all([
    request<ScheduleItem[]>('/api/enrollment/timetable'),
    request<(TeacherItem | StudentItem)[]>(`/api/enrollment/${roleResource}`),
  ]);
  const enrollment = roleEnrollments.find(item => item.id === baseProfile.id);
  const profile = { ...baseProfile, values: { ...baseProfile.values, ...enrollment?.values } } as TeacherItem | StudentItem;
  if (session.role === 'student') {
    const student = profile as StudentItem;
    const [attendance, grades] = await Promise.all([
      request<AttendanceItem[]>('/api/catalog/attendance'),
      request<GradeItem[]>('/api/catalog/grades'),
    ]);
    return {
      role: session.role,
      profile,
      schedule: schedule.filter(item => (!item.values.departmentId || item.values.departmentId === student.values.departmentId) && item.values.yearLevel === student.values.year),
      students: [],
      attendance: attendance.filter(item => item.values.studentId === student.id),
      grades: grades.filter(item => item.values.studentId === student.id),
      gradeWeights,
      announcements,
    };
  }

  const teacher = profile as TeacherItem;
  const ownSchedule = schedule.filter(item => item.values.teacherId === teacher.id);
  const [students, attendance, grades] = await Promise.all([
    request<StudentItem[]>('/api/enrollment/students'),
    request<AttendanceItem[]>('/api/catalog/attendance'),
    request<GradeItem[]>('/api/catalog/grades'),
  ]);
  const assignedStudents = students.filter(student => ownSchedule.some(item =>
    (!item.values.departmentId || item.values.departmentId === student.values.departmentId) && item.values.yearLevel === student.values.year));
  const studentIds = new Set(assignedStudents.map(student => student.id));
  const courseIds = new Set(ownSchedule.map(item => item.values.courseId));
  return {
    role: session.role,
    profile,
    schedule: ownSchedule,
    students: assignedStudents,
    attendance: attendance.filter(item => studentIds.has(item.values.studentId)),
    grades: grades.filter(item => studentIds.has(item.values.studentId) && courseIds.has(item.values.courseId)),
    gradeWeights,
    announcements,
  };
}

export const portalMutations = {
  recordAttendance: (studentId: string, status: string) => request<void>('/api/attendance', { method: 'POST', body: JSON.stringify({ studentId, status }) }),
  submitGrade: (studentId: string, courseId: string, scores: { assignmentScore: number; midtermScore: number; finalExamScore: number }) => request<void>('/api/grades', { method: 'POST', body: JSON.stringify({ studentId, courseId, ...scores }) }),
};
