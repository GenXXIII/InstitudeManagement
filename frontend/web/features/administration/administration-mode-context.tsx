"use client";

import { createContext, useContext, useMemo, useState } from "react";

type AdministrationModeContextValue = {
  advanced: boolean;
  setAdvanced: (advanced: boolean) => void;
};

const AdministrationModeContext = createContext<AdministrationModeContextValue | null>(null);

export function AdministrationModeProvider({ children }: { children: React.ReactNode }) {
  const [advanced, setAdvanced] = useState(false);
  const value = useMemo(() => ({ advanced, setAdvanced }), [advanced]);
  return <AdministrationModeContext.Provider value={value}>{children}</AdministrationModeContext.Provider>;
}

export function useAdministrationMode() {
  const value = useContext(AdministrationModeContext);
  if (!value) throw new Error("useAdministrationMode must be used inside AdministrationModeProvider.");
  return value;
}
