import { createContext, PropsWithChildren, useCallback, useContext, useEffect, useMemo, useState } from 'react';

import { clearStoredSession, readStoredSession, writeStoredSession } from './auth-storage';

export type MobileRole = 'teacher' | 'student';
export type MobileSession = { role: MobileRole; email: string };

type AuthContextValue = {
  ready: boolean;
  session: MobileSession | null;
  signIn: (email: string, password: string) => Promise<MobileSession>;
  signOut: () => Promise<void>;
};

const accounts: MobileSession[] = [
  { role: 'teacher', email: 'teacher@gmail.com' },
  { role: 'student', email: 'studnet@gmail.com' },
];

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const [ready, setReady] = useState(false);
  const [session, setSession] = useState<MobileSession | null>(null);

  useEffect(() => {
    readStoredSession()
      .then(value => {
        if (!value) return;
        const saved = JSON.parse(value) as MobileSession;
        if (accounts.some(account => account.role === saved.role && account.email === saved.email)) setSession(saved);
      })
      .catch(() => undefined)
      .finally(() => setReady(true));
  }, []);

  const signIn = useCallback(async (email: string, password: string) => {
    const normalizedEmail = email.trim().toLowerCase();
    const account = accounts.find(candidate => candidate.email === normalizedEmail);
    if (!account || password !== '1234') throw new Error('Email or password is incorrect.');
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

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider.');
  return context;
}
