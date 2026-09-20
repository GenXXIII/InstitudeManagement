import Constants from 'expo-constants';
import * as Device from 'expo-device';
import { Platform } from 'react-native';
import type { MobileSession } from '@/features/auth/auth-context';
import type { Announcement, AttendanceItem, ClassSessionStartItem, GradeItem, GradeWeights, PortalData, ScheduleItem, StudentFinanceOptions, StudentItem, StudentPayment, TeacherItem } from './portal-types';

const configuredApiUrl = process.env.EXPO_PUBLIC_API_URL?.trim().replace(/\/$/, '');

export function getApiBaseUrl() {
  if (configuredApiUrl) return configuredApiUrl;
  if (Platform.OS === 'web' && typeof window !== 'undefined') return `http://${window.location.hostname}:5080`;
  let expoHost = readHost(Constants.expoConfig?.hostUri)
    || readHost(Constants.expoGoConfig?.debuggerHost)
    || readHost(Constants.linkingUri);
  if (Platform.OS === 'android' && !Device.isDevice && (!expoHost || expoHost === 'localhost' || expoHost === '127.0.0.1')) expoHost = '10.0.2.2';
  return expoHost ? `http://${expoHost}:5080` : 'http://localhost:5080';
}

function readHost(value?: string | null) {
  if (!value) return '';
  try {
    const url = new URL(value.includes('://') ? value : `http://${value}`);
    return url.hostname.replace(/^\[|\]$/g, '');
  } catch {
    return value.split(':')[0];
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${getApiBaseUrl()}${path}`, {
      ...init,
      headers: { Accept: 'application/json', 'Content-Type': 'application/json', ...init?.headers },
    });
  } catch {
    throw new Error(`Cannot connect to the Institute API at ${getApiBaseUrl()}. Keep the phone and computer on the same Wi-Fi network, then retry.`);
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
  if (response.status === 204) return undefined as T;
  const body = await response.text();
  return body.trim() ? JSON.parse(body) as T : undefined as T;
}

export function signInMobile(publicId: string, password: string) {
  return request<MobileSession>('/api/mobile/auth/sign-in', {
    method: 'POST',
    body: JSON.stringify({ publicId, password }),
  });
}

export async function loadPortalData(session: MobileSession): Promise<PortalData> {
  const roleResource = session.role === 'teacher' ? 'teachers' : 'students';
  const profiles = await request<TeacherItem[] | StudentItem[]>(`/api/catalog/${roleResource}?search=${encodeURIComponent(session.publicId)}`);
  const baseProfile = profiles.find(item => item.id === session.profileId && item.values.publicId === session.publicId) ?? null;
  const [announcementRows, gradeSettings] = await Promise.all([
    request<Omit<Announcement, 'source' | 'sourceId'>[]>('/api/notification-center/alerts'),
    request<{ values: Record<string, string> }>('/api/settings/grade-rules'),
  ]);
  const gradeWeights: GradeWeights = {
    attendance: Number(gradeSettings.values.attendanceWeight || 10),
    assignment: Number(gradeSettings.values.assignmentWeight || 20),
    midterm: Number(gradeSettings.values.midtermWeight || 20),
    finalExam: Number(gradeSettings.values.finalExamWeight || 50),
  };
  const announcements: Announcement[] = announcementRows.map(item => ({ ...item, source: 'announcement', sourceId: item.id }));
  if (!baseProfile) return { role: session.role, profile: null, schedule: [], students: [], attendance: [], grades: [], gradeWeights, announcements, payments: [], financeOptions: emptyFinanceOptions(), startedScheduleIds: [] };

  const [scheduleRows, roleEnrollments] = await Promise.all([
    request<ScheduleItem[]>('/api/enrollment/timetable'),
    request<(TeacherItem | StudentItem)[]>(`/api/enrollment/${roleResource}`),
  ]);
  const schedule = scheduleRows.filter(item => item.values.periodState === 'Current');
  const enrollment = roleEnrollments.find(item => item.id === baseProfile.id && item.values.periodState === 'Current');
  const profile = { ...baseProfile, values: { ...baseProfile.values, ...enrollment?.values } } as TeacherItem | StudentItem;
  if (session.role === 'student') {
    const student = profile as StudentItem;
    const [attendance, grades, payments, financeOptions] = await Promise.all([
      request<AttendanceItem[]>('/api/catalog/attendance'),
      request<GradeItem[]>('/api/catalog/grades'),
      request<StudentPayment[]>(`/api/finance/students/${student.id}`),
      request<StudentFinanceOptions>('/api/finance/options'),
    ]);
    const financeAlerts = payments.filter(payment => payment.reminderSentAtUtc && payment.status !== 'Cancelled').map(payment => ({
      id: `finance-${payment.id}`,
      source: 'finance' as const,
      sourceId: payment.id,
      announcementCode: payment.paymentCode,
      type: 'Finance' as const,
      title: payment.status === 'Paid' ? `${payment.paymentPlan} payment confirmed` : payment.title,
      message: payment.status === 'Paid'
        ? `${payment.totalPaid} ${payment.currency} was confirmed for ${payment.paymentPlan === 'Year' ? 'Semester 1 and Semester 2' : payment.semester}.`
        : `Please pay the remaining ${payment.balance} ${payment.currency} for ${payment.paymentPlan === 'Year' ? 'Semester 1 and Semester 2' : payment.semester}. Your next enrollment is held until Finance reports Paid.`,
      isRead: Boolean(payment.reminderReadAtUtc),
      createAt: payment.reminderSentAtUtc!,
    }));
    return {
      role: session.role,
      profile,
      schedule: enrollment ? schedule.filter(item => (!item.values.departmentId || item.values.departmentId === student.values.departmentId) && item.values.yearLevel === student.values.year) : [],
      students: [],
      attendance: attendance.filter(item => item.values.studentId === student.id),
      grades: grades.filter(item => item.values.studentId === student.id),
      gradeWeights,
      announcements: [...financeAlerts, ...announcements].sort((left, right) => right.createAt.localeCompare(left.createAt)),
      payments,
      financeOptions,
      startedScheduleIds: [],
    };
  }

  const teacher = profile as TeacherItem;
  const ownSchedule = schedule.filter(item => item.values.teacherId === teacher.id);
  const [students, attendance, grades, classStarts] = await Promise.all([
    request<StudentItem[]>('/api/enrollment/students'),
    request<AttendanceItem[]>('/api/catalog/attendance'),
    request<GradeItem[]>('/api/catalog/grades'),
    request<ClassSessionStartItem[]>(`/api/mobile/classes/teachers/${teacher.id}/today`),
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
    payments: [],
    financeOptions: emptyFinanceOptions(),
    startedScheduleIds: classStarts.map(item => item.scheduleEntryId),
  };
}

export const portalMutations = {
  startClass: (scheduleEntryId: string, teacherId: string) => request<ClassSessionStartItem>(`/api/mobile/classes/${scheduleEntryId}/start`, { method: 'POST', body: JSON.stringify({ teacherId }) }),
  recordAttendance: (studentId: string, status: string) => request<void>('/api/attendance', { method: 'POST', body: JSON.stringify({ studentId, status }) }),
  submitGrade: (studentId: string, courseId: string, scores: { assignmentScore: number; midtermScore: number; finalExamScore: number }) => request<void>('/api/grades', { method: 'POST', body: JSON.stringify({ studentId, courseId, ...scores }) }),
  markAnnouncementRead: (announcementId: string) => request<Announcement>(`/api/notification-center/alerts/${announcementId}/read`, { method: 'PUT' }),
  markFinanceReminderRead: (studentId: string, paymentId: string) => request<StudentPayment>(`/api/finance/students/${studentId}/payments/${paymentId}/reminder/read`, { method: 'PUT' }),
  generateFinanceQr: (studentId: string, paymentId: string) => request<StudentPayment>(`/api/finance/students/${studentId}/payments/${paymentId}/qr`, { method: 'PUT' }),
  verifyFinancePayment: (studentId: string, paymentId: string) => request<StudentPayment>(`/api/finance/students/${studentId}/payments/${paymentId}/verify`, { method: 'POST' }),
};

function emptyFinanceOptions(): StudentFinanceOptions {
  return { paymentProviders: [], bakongEnabled: false, bakongConfigured: false, bakongEnvironment: 'SIT', dynamicQrBank: '', dynamicQrAccountName: '', dynamicQrAccountCode: '' };
}
