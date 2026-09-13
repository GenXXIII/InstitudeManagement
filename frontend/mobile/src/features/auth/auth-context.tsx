import { createContext, PropsWithChildren, useCallback, useContext, useEffect, useMemo, useState } from 'react';

import { signInMobile } from '@/features/portal/portal-api';
import { clearStoredSession, readStoredSession, writeStoredSession } from './auth-storage';

export type MobileRole = 'teacher' | 'student';
export type MobileSession = { role: MobileRole; publicId: string; profileId: string };

type AuthContextValue = {
  ready: boolean;
  session: MobileSession | null;
  signIn: (publicId: string, password: string) => Promise<MobileSession>;
  signOut: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const [ready, setReady] = useState(false);
  const [session, setSession] = useState<MobileSession | null>(null);

  useEffect(() => {
    readStoredSession()
      .then(value => {
        if (!value) return;
        const saved = JSON.parse(value) as MobileSession;
        if (isMobileSession(saved)) setSession(saved);
      })
      .catch(() => undefined)
      .finally(() => setReady(true));
  }, []);

  const signIn = useCallback(async (publicId: string, password: string) => {
    const account = await signInMobile(publicId.trim(), password);
    await writeStoredSession(JSON.stringify(account));
    setSession(account);
    return account;
  }, []);

  const signOut = useCallback(async () => {
    await clearStoredSession();
    setSession(null);
  }, []);

  const value = useMemo(() => ({ ready, session, signIn, signOut }), [ready, session, signIn, signOut]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

function isMobileSession(value: MobileSession) {
  return (value.role === 'teacher' || value.role === 'student')
    && typeof value.publicId === 'string' && value.publicId.length > 0
    && typeof value.profileId === 'string' && value.profileId.length > 0;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider.');
  return context;
}
