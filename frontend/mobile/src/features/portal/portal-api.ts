import Constants from 'expo-constants';
import * as Device from 'expo-device';
import { Platform } from 'react-native';
import type { MobileSession } from '@/features/auth/auth-context';
import type { Announcement, AttendanceItem, ClassAttendanceCheckIn, ClassPermissionRequestItem, ClassSessionStartItem, CourseItem, GradeItem, GradeWeights, PortalData, PublishedSemesterResult, ScheduleItem, StudentFinanceOptions, StudentItem, StudentPayment, TeacherItem } from './portal-types';

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

type PagedResult<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number };

async function requestItems<T>(path: string) {
  const separator = path.includes('?') ? '&' : '?';
  return (await request<PagedResult<T>>(`${path}${separator}page=1&pageSize=100`)).items;
}

async function requestAllItems<T>(path: string) {
  const items: T[] = [];
  let page = 1;
  let totalPages = 1;
  const separator = path.includes('?') ? '&' : '?';
  do {
    const result = await request<PagedResult<T>>(`${path}${separator}page=${page}&pageSize=100`);
    items.push(...result.items);
    totalPages = result.totalPages;
    page += 1;
  } while (page <= totalPages);
  return items;
}

export function signInMobile(publicId: string, password: string) {
  return request<MobileSession>('/api/mobile/auth/sign-in', {
    method: 'POST',
    body: JSON.stringify({ publicId, password }),
  });
}

export async function loadPortalData(session: MobileSession): Promise<PortalData> {
  const roleResource = session.role === 'teacher' ? 'teachers' : 'students';
  const profiles = await requestItems<TeacherItem | StudentItem>(`/api/catalog/${roleResource}?profileId=${encodeURIComponent(session.profileId)}`);
  const baseProfile = profiles.find(item => item.id === session.profileId) ?? null;
  const [announcementRows, gradeSettings] = await Promise.all([
    requestItems<Omit<Announcement, 'source' | 'sourceId'>>('/api/notification-center/alerts'),
    request<{ values: Record<string, string> }>('/api/settings/grade-rules'),
  ]);
  const gradeWeights: GradeWeights = {
    attendance: Number(gradeSettings.values.attendanceWeight || 10),
    assignment: Number(gradeSettings.values.assignmentWeight || 20),
    midterm: Number(gradeSettings.values.midtermWeight || 20),
    finalExam: Number(gradeSettings.values.finalExamWeight || 50),
  };
  const announcements: Announcement[] = announcementRows.map(item => ({ ...item, source: 'announcement', sourceId: item.id }));
  if (!baseProfile) return { role: session.role, profile: null, courses: [], schedule: [], students: [], attendance: [], grades: [], publishedResults: [], gradeWeights, announcements, payments: [], financeOptions: emptyFinanceOptions(), startedScheduleIds: [], permissionRequests: [] };

  const roleEnrollments = await requestItems<TeacherItem | StudentItem>(`/api/enrollment/${roleResource}?search=${encodeURIComponent(session.publicId)}`);
  const enrollment = roleEnrollments.find(item => item.id === baseProfile.id && item.values.periodState === 'Current');
  const courseParams = new URLSearchParams();
  const scheduleParams = new URLSearchParams();
  if (session.role === 'teacher') {
    scheduleParams.set('search', baseProfile.values.name);
  } else if (enrollment) {
    courseParams.set('year', enrollment.values.year);
    scheduleParams.set('year', enrollment.values.year);
    scheduleParams.set('search', enrollment.values.shift);
    if (enrollment.values.year !== '1' && enrollment.values.departmentId) {
      courseParams.set('departmentId', enrollment.values.departmentId);
      scheduleParams.set('departmentId', enrollment.values.departmentId);
    }
  }
  const [courseRows, scheduleRows] = await Promise.all([
    session.role === 'teacher' ? Promise.resolve([] as CourseItem[]) : requestAllItems<CourseItem>(`/api/enrollment/courses?${courseParams}`),
    requestAllItems<ScheduleItem>(`/api/enrollment/timetable?${scheduleParams}`),
  ]);
  const schedule = scheduleRows.filter(item => item.values.periodState === 'Current');
  const profile = { ...baseProfile, values: { ...baseProfile.values, ...enrollment?.values } } as TeacherItem | StudentItem;
  if (session.role === 'student') {
    const student = profile as StudentItem;
    const isGeneralYear = enrollment?.values.year === '1';
    const studentCourses = enrollment ? courseRows.filter(item =>
      item.values.periodState === 'Current'
      && item.values.status === 'Active'
      && item.values.year === enrollment.values.year
      && (isGeneralYear || item.values.departmentId === enrollment.values.departmentId)) : [];
    const studentSchedule = enrollment ? schedule.filter(item =>
      item.values.yearLevel === enrollment.values.year
      && (isGeneralYear || item.values.departmentId === enrollment.values.departmentId)
      && item.values.shift === enrollment.values.shift) : [];
    const [attendance, payments, financeOptions, classStarts, permissionRequests, publishedResults] = await Promise.all([
      requestAllItems<AttendanceItem>(`/api/catalog/attendance?search=${encodeURIComponent(student.values.studentCode)}`),
      request<StudentPayment[]>(`/api/finance/students/${student.id}`),
      request<StudentFinanceOptions>('/api/finance/options'),
      request<ClassSessionStartItem[]>(`/api/mobile/classes/students/${student.id}/today`),
      request<ClassPermissionRequestItem[]>(`/api/mobile/classes/students/${student.id}/permission-requests`),
      requestAllItems<PublishedSemesterResult>(`/api/results?studentId=${student.id}&publishedOnly=true&history=false`),
    ]);
    const financeAlerts = payments.filter(payment => payment.reminderSentAtUtc && payment.status !== 'Cancelled').map(payment => ({
      id: `finance-${payment.id}`,
      source: 'finance' as const,
      sourceId: payment.id,
      announcementCode: payment.paymentCode,
      type: 'Finance' as const,
      title: payment.status === 'Paid' ? `${payment.paymentPlan} payment confirmed` : payment.title,
      message: payment.status === 'Paid'
        ? `${payment.totalPaid} ${payment.currency} was confirmed for ${payment.semester}.`
        : `Please pay the remaining ${payment.balance} ${payment.currency} for ${payment.semester}. Your next enrollment is held until Finance reports Paid.`,
      isRead: Boolean(payment.reminderReadAtUtc),
      createAt: payment.reminderSentAtUtc!,
    }));
    return {
      role: session.role,
      profile,
      courses: studentCourses,
      schedule: studentSchedule,
      students: [],
      attendance: attendance.filter(item => item.values.studentId === student.id),
      grades: [],
      publishedResults,
      gradeWeights,
      announcements: [...financeAlerts, ...announcements].sort((left, right) => right.createAt.localeCompare(left.createAt)),
      payments,
      financeOptions,
      startedScheduleIds: classStarts.map(item => item.scheduleEntryId),
      permissionRequests,
    };
  }

  const teacher = profile as TeacherItem;
  const ownSchedule = schedule.filter(item => item.values.teacherId === teacher.id);
  const cohortParams = [...new Map(ownSchedule.map(item => {
    const params = new URLSearchParams({ year: item.values.yearLevel, search: item.values.shift });
    if (item.values.yearLevel !== '1' && item.values.departmentId) params.set('departmentId', item.values.departmentId);
    return [params.toString(), params] as const;
  })).values()];
  const [studentGroups, grades, classStarts, permissionRequests] = await Promise.all([
    Promise.all(cohortParams.map(params => requestAllItems<StudentItem>(`/api/enrollment/students?${params}`))),
    requestAllItems<GradeItem>(`/api/catalog/grades?teacherId=${teacher.id}`),
    request<ClassSessionStartItem[]>(`/api/mobile/classes/teachers/${teacher.id}/today`),
    request<ClassPermissionRequestItem[]>(`/api/mobile/classes/teachers/${teacher.id}/permission-requests`),
  ]);
  const studentsById = new Map<string, StudentItem>();
  for (const student of studentGroups.flat()) {
    const existing = studentsById.get(student.id);
    if (!existing || student.values.periodState === 'Current') studentsById.set(student.id, student);
  }
  const students = [...studentsById.values()];
  const assignedStudents = students.filter(student => student.values.periodState === 'Current' && ownSchedule.some(item =>
    item.values.yearLevel === student.values.year
    && (student.values.year === '1' || item.values.departmentId === student.values.departmentId)
    && item.values.shift === student.values.shift));
  const attendanceGroups = await Promise.all(cohortParams.map(params => {
    const attendanceParams = new URLSearchParams();
    const year = params.get('year');
    const shift = params.get('search');
    const departmentId = params.get('departmentId');
    if (year) attendanceParams.set('year', year);
    if (shift) attendanceParams.set('shift', shift);
    if (departmentId) attendanceParams.set('departmentId', departmentId);
    return requestAllItems<AttendanceItem>(`/api/catalog/attendance?${attendanceParams}`);
  }));
  const attendance = [...new Map(attendanceGroups.flat().map(item => [item.id, item])).values()];
  const studentIds = new Set(assignedStudents.map(student => student.id));
  const courseIds = new Set(ownSchedule.map(item => item.values.courseId));
  return {
    role: session.role,
    profile,
    courses: [],
    schedule: ownSchedule,
    students: assignedStudents,
    attendance: attendance.filter(item => studentIds.has(item.values.studentId)),
    grades: grades.filter(item => studentIds.has(item.values.studentId) && courseIds.has(item.values.courseId)),
    publishedResults: [],
    gradeWeights,
    announcements,
    payments: [],
    financeOptions: emptyFinanceOptions(),
    startedScheduleIds: classStarts.map(item => item.scheduleEntryId),
    permissionRequests,
  };
}

export const portalMutations = {
  startClass: (scheduleEntryId: string, teacherId: string, qrPayload: string) => request<ClassSessionStartItem>(`/api/mobile/classes/${scheduleEntryId}/start`, { method: 'POST', body: JSON.stringify({ teacherId, qrPayload }) }),
  getTodayClassStarts: (role: 'teacher' | 'student', profileId: string) => request<ClassSessionStartItem[]>(`/api/mobile/classes/${role}s/${profileId}/today`),
  checkInClass: (scheduleEntryId: string, studentId: string, qrPayload: string) => request<ClassAttendanceCheckIn>(`/api/mobile/classes/${scheduleEntryId}/attendance/check-in`, { method: 'POST', body: JSON.stringify({ studentId, qrPayload }) }),
  requestCourseSubmission: (courseId: string, teacherId: string, students: { studentId: string; assignmentScore: number; midtermScore: number; finalExamScore: number }[]) => request<void>('/api/grades/course-submissions/request', { method: 'POST', body: JSON.stringify({ courseId, teacherId, students }) }),
  requestPermission: (studentId: string, sessionDate: string, reason: string) => request<ClassPermissionRequestItem>(`/api/mobile/classes/students/${studentId}/permission-requests`, { method: 'POST', body: JSON.stringify({ sessionDate, reason }) }),
  reviewPermission: (requestId: string, teacherId: string, decision: 'Approved' | 'Rejected') => request<ClassPermissionRequestItem>(`/api/mobile/classes/permission-requests/${requestId}/decision`, { method: 'PUT', body: JSON.stringify({ teacherId, decision }) }),
  submitAuthorizedCourse: (gradeId: string, teacherId: string, students: { studentId: string; assignmentScore: number; midtermScore: number; finalExamScore: number }[] = []) => request<void>(`/api/grades/${gradeId}/course-submit`, { method: 'POST', body: JSON.stringify({ teacherId, students }) }),
  requestCourseResubmission: (gradeId: string, teacherId: string, note = '') => request<void>(`/api/grades/${gradeId}/course-resubmission-request`, { method: 'POST', body: JSON.stringify({ teacherId, note }) }),
  markAnnouncementRead: (announcementId: string) => request<Announcement>(`/api/notification-center/alerts/${announcementId}/read`, { method: 'PUT' }),
  markFinanceReminderRead: (studentId: string, paymentId: string) => request<StudentPayment>(`/api/finance/students/${studentId}/payments/${paymentId}/reminder/read`, { method: 'PUT' }),
  generateFinanceQr: (studentId: string, paymentId: string) => request<StudentPayment>(`/api/finance/students/${studentId}/payments/${paymentId}/qr`, { method: 'PUT' }),
  verifyFinancePayment: (studentId: string, paymentId: string) => request<StudentPayment>(`/api/finance/students/${studentId}/payments/${paymentId}/verify`, { method: 'POST' }),
  scanMockFinanceQr: (studentId: string, paymentId: string, qrPayload: string) => request<StudentPayment>(`/api/finance/students/${studentId}/payments/${paymentId}/mock-scan`, { method: 'POST', body: JSON.stringify({ qrPayload }) }),
};

function emptyFinanceOptions(): StudentFinanceOptions {
  return { bakongEnabled: false, bakongConfigured: false, bakongEnvironment: 'SIT', dynamicQrBank: '', dynamicQrAccountName: '', dynamicQrAccountCode: '', mockPaymentEnabled: false };
}
