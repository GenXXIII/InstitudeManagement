import { createContext, PropsWithChildren, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { useAuth, type MobileRole } from '@/features/auth/auth-context';
import { loadPortalData, portalMutations } from './portal-api';
import type { PortalData, StudentPayment } from './portal-types';

type GradeScores = { assignmentScore: number; midtermScore: number; finalExamScore: number };
type GradeSubmission = { studentId: string; courseId: string; scores: GradeScores };

type PortalContextValue = PortalData & {
  error: string;
  loading: boolean;
  refresh: () => Promise<void>;
  startClass: (scheduleEntryId: string, teacherId: string) => Promise<void>;
  recordAttendance: (studentId: string, status: string) => Promise<void>;
  submitGrade: (studentId: string, courseId: string, scores: GradeScores) => Promise<void>;
  submitGrades: (submissions: GradeSubmission[]) => Promise<void>;
  markAnnouncementRead: (announcementId: string) => Promise<void>;
  generateFinanceQr: (studentId: string, paymentId: string) => Promise<StudentPayment>;
  verifyFinancePayment: (studentId: string, paymentId: string) => Promise<StudentPayment>;
};

const PortalContext = createContext<PortalContextValue | null>(null);

export function PortalProvider({ role, children }: PropsWithChildren<{ role: MobileRole }>) {
  const { session } = useAuth();
  const [data, setData] = useState<PortalData>({ role, profile: null, schedule: [], students: [], attendance: [], grades: [], gradeWeights: { attendance: 10, assignment: 20, midterm: 20, finalExam: 50 }, announcements: [], payments: [], financeOptions: { paymentProviders: [], bakongEnabled: false, bakongConfigured: false, bakongEnvironment: 'SIT', dynamicQrBank: '', dynamicQrAccountName: '', dynamicQrAccountCode: '' }, startedScheduleIds: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const refresh = useCallback(async () => {
    if (!session || session.role !== role) return;
    setLoading(true);
    setError('');
    try { setData(await loadPortalData(session)); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Could not load the mobile workspace.'); }
    finally { setLoading(false); }
  }, [role, session]);

  useEffect(() => {
    const loadTimer = setTimeout(() => void refresh(), 0);
    return () => clearTimeout(loadTimer);
  }, [refresh]);

  const recordAttendance = useCallback(async (studentId: string, status: string) => {
    await portalMutations.recordAttendance(studentId, status);
    await refresh();
  }, [refresh]);

  const submitGrade = useCallback(async (studentId: string, courseId: string, scores: GradeScores) => {
    await portalMutations.submitGrade(studentId, courseId, scores);
    await refresh();
  }, [refresh]);

  const submitGrades = useCallback(async (submissions: GradeSubmission[]) => {
    const batchSize = 8;
    for (let index = 0; index < submissions.length; index += batchSize) {
      const batch = submissions.slice(index, index + batchSize);
      await Promise.all(batch.map(item => portalMutations.submitGrade(item.studentId, item.courseId, item.scores)));
    }
    await refresh();
  }, [refresh]);

  const startClass = useCallback(async (scheduleEntryId: string, teacherId: string) => {
    const started = await portalMutations.startClass(scheduleEntryId, teacherId);
    setData(current => ({
      ...current,
      startedScheduleIds: current.startedScheduleIds.includes(started.scheduleEntryId)
        ? current.startedScheduleIds
        : [...current.startedScheduleIds, started.scheduleEntryId],
    }));
  }, []);

  const replacePayment = useCallback((payment: StudentPayment) => {
    setData(current => ({ ...current, payments: current.payments.map(item => item.id === payment.id ? payment : item) }));
    return payment;
  }, []);

  const generateFinanceQr = useCallback(async (studentId: string, paymentId: string) =>
    replacePayment(await portalMutations.generateFinanceQr(studentId, paymentId)), [replacePayment]);

  const verifyFinancePayment = useCallback(async (studentId: string, paymentId: string) =>
    replacePayment(await portalMutations.verifyFinancePayment(studentId, paymentId)), [replacePayment]);

  const markAnnouncementRead = useCallback(async (announcementId: string) => {
    const announcement = data.announcements.find(item => item.id === announcementId);
    if (!announcement || announcement.isRead) return;

    setData(current => ({
      ...current,
      announcements: current.announcements.map(item => item.id === announcementId ? { ...item, isRead: true } : item),
    }));
    try {
      if (announcement.source === 'finance' && data.profile) await portalMutations.markFinanceReminderRead(data.profile.id, announcement.sourceId);
      else await portalMutations.markAnnouncementRead(announcement.sourceId);
    }
    catch (reason) {
      setData(current => ({
        ...current,
        announcements: current.announcements.map(item => item.id === announcementId ? { ...item, isRead: false } : item),
      }));
      setError(reason instanceof Error ? reason.message : 'Could not mark the notification as read.');
    }
  }, [data.announcements, data.profile]);

  const value = useMemo(() => ({ ...data, error, loading, refresh, startClass, recordAttendance, submitGrade, submitGrades, markAnnouncementRead, generateFinanceQr, verifyFinancePayment }), [data, error, loading, refresh, startClass, recordAttendance, submitGrade, submitGrades, markAnnouncementRead, generateFinanceQr, verifyFinancePayment]);
  return <PortalContext.Provider value={value}>{children}</PortalContext.Provider>;
}

export function usePortal() {
  const context = useContext(PortalContext);
  if (!context) throw new Error('usePortal must be used inside PortalProvider.');
  return context;
}
