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
  submitGrade: (studentId: string, courseId: string, scores: GradeScores) => Promise<void>;
  submitGrades: (submissions: GradeSubmission[]) => Promise<void>;
  requestPermission: (sessionDate: string, reason: string) => Promise<void>;
  reviewPermission: (requestId: string, decision: 'Approved' | 'Rejected') => Promise<void>;
  requestGradeResubmission: (gradeId: string) => Promise<void>;
  markAnnouncementRead: (announcementId: string) => Promise<void>;
  generateFinanceQr: (studentId: string, paymentId: string) => Promise<StudentPayment>;
  verifyFinancePayment: (studentId: string, paymentId: string) => Promise<StudentPayment>;
};

const PortalContext = createContext<PortalContextValue | null>(null);

export function PortalProvider({ role, children }: PropsWithChildren<{ role: MobileRole }>) {
  const { session } = useAuth();
  const [data, setData] = useState<PortalData>({ role, profile: null, schedule: [], students: [], attendance: [], grades: [], publishedResults: [], gradeWeights: { attendance: 10, assignment: 20, midterm: 20, finalExam: 50 }, announcements: [], payments: [], financeOptions: { paymentProviders: [], bakongEnabled: false, bakongConfigured: false, bakongEnvironment: 'SIT', dynamicQrBank: '', dynamicQrAccountName: '', dynamicQrAccountCode: '' }, startedScheduleIds: [], permissionRequests: [] });
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

  const submitGrade = useCallback(async (studentId: string, courseId: string, scores: GradeScores) => {
    if (!data.profile) throw new Error('Teacher profile is unavailable.');
    await portalMutations.submitGrade(studentId, courseId, data.profile.id, scores);
    await refresh();
  }, [data.profile, refresh]);

  const submitGrades = useCallback(async (submissions: GradeSubmission[]) => {
    const batchSize = 8;
    for (let index = 0; index < submissions.length; index += batchSize) {
      const batch = submissions.slice(index, index + batchSize);
      if (!data.profile) throw new Error('Teacher profile is unavailable.');
      await Promise.all(batch.map(item => portalMutations.submitGrade(item.studentId, item.courseId, data.profile!.id, item.scores)));
    }
    await refresh();
  }, [data.profile, refresh]);

  const requestPermission = useCallback(async (sessionDate: string, reason: string) => {
    if (!data.profile) throw new Error('Student profile is unavailable.');
    await portalMutations.requestPermission(data.profile.id, sessionDate, reason);
    await refresh();
  }, [data.profile, refresh]);

  const requestGradeResubmission = useCallback(async (gradeId: string) => {
    if (!data.profile) throw new Error('Teacher profile is unavailable.');
    await portalMutations.requestGradeResubmission(gradeId, data.profile.id);
    await refresh();
  }, [data.profile, refresh]);

  const reviewPermission = useCallback(async (requestId: string, decision: 'Approved' | 'Rejected') => {
    if (!data.profile) throw new Error('Teacher profile is unavailable.');
    await portalMutations.reviewPermission(requestId, data.profile.id, decision);
    await refresh();
  }, [data.profile, refresh]);

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

  const value = useMemo(() => ({ ...data, error, loading, refresh, startClass, submitGrade, submitGrades, requestPermission, reviewPermission, requestGradeResubmission, markAnnouncementRead, generateFinanceQr, verifyFinancePayment }), [data, error, loading, refresh, startClass, submitGrade, submitGrades, requestPermission, reviewPermission, requestGradeResubmission, markAnnouncementRead, generateFinanceQr, verifyFinancePayment]);
  return <PortalContext.Provider value={value}>{children}</PortalContext.Provider>;
}

export function usePortal() {
  const context = useContext(PortalContext);
  if (!context) throw new Error('usePortal must be used inside PortalProvider.');
  return context;
}
