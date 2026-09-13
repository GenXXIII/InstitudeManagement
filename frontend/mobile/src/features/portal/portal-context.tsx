import { createContext, PropsWithChildren, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { useAuth, type MobileRole } from '@/features/auth/auth-context';
import { loadPortalData, portalMutations } from './portal-api';
import type { PortalData } from './portal-types';

type PortalContextValue = PortalData & {
  error: string;
  loading: boolean;
  refresh: () => Promise<void>;
  recordAttendance: (studentId: string, status: string) => Promise<void>;
  submitGrade: (studentId: string, courseId: string, scores: { assignmentScore: number; midtermScore: number; finalExamScore: number }) => Promise<void>;
};

const PortalContext = createContext<PortalContextValue | null>(null);

export function PortalProvider({ role, children }: PropsWithChildren<{ role: MobileRole }>) {
  const { session } = useAuth();
  const [data, setData] = useState<PortalData>({ role, profile: null, schedule: [], students: [], attendance: [], grades: [], gradeWeights: { attendance: 10, assignment: 20, midterm: 20, finalExam: 50 }, announcements: [] });
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

  const submitGrade = useCallback(async (studentId: string, courseId: string, scores: { assignmentScore: number; midtermScore: number; finalExamScore: number }) => {
    await portalMutations.submitGrade(studentId, courseId, scores);
    await refresh();
  }, [refresh]);

  const value = useMemo(() => ({ ...data, error, loading, refresh, recordAttendance, submitGrade }), [data, error, loading, refresh, recordAttendance, submitGrade]);
  return <PortalContext.Provider value={value}>{children}</PortalContext.Provider>;
}

export function usePortal() {
  const context = useContext(PortalContext);
  if (!context) throw new Error('usePortal must be used inside PortalProvider.');
  return context;
}
