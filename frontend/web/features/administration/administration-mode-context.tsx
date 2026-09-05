"use client";

import { createContext, useCallback, useContext, useMemo, useSyncExternalStore } from "react";

const advancedModeStorageKey = "ink-administration-advanced-options";
const advancedModeChangeEvent = "ink-administration-mode-change";
let advancedModeFallback = false;

type AdministrationModeContextValue = {
  advanced: boolean;
  setAdvanced: (advanced: boolean) => void;
};

const AdministrationModeContext = createContext<AdministrationModeContextValue | null>(null);

export function AdministrationModeProvider({ children }: { children: React.ReactNode }) {
  const advanced = useSyncExternalStore(subscribeToAdvancedMode, readAdvancedMode, () => false);
  const setAdvanced = useCallback((enabled: boolean) => {
    advancedModeFallback = enabled;
    try { window.localStorage.setItem(advancedModeStorageKey, String(enabled)); }
    catch { /* Browser storage can be unavailable in restricted browsing modes. */ }
    window.dispatchEvent(new Event(advancedModeChangeEvent));
  }, []);
  const value = useMemo(() => ({ advanced, setAdvanced }), [advanced, setAdvanced]);
  return <AdministrationModeContext.Provider value={value}>{children}</AdministrationModeContext.Provider>;
}

export function useAdministrationMode() {
  const value = useContext(AdministrationModeContext);
  if (!value) throw new Error("useAdministrationMode must be used inside AdministrationModeProvider.");
  return value;
}

function readAdvancedMode() {
  try {
    const stored = window.localStorage.getItem(advancedModeStorageKey);
    return stored === null ? advancedModeFallback : stored === "true";
  }
  catch { return advancedModeFallback; }
}

function subscribeToAdvancedMode(notify: () => void) {
  const handleStorage = (event: StorageEvent) => {
    if (!event.key || event.key === advancedModeStorageKey) notify();
  };
  window.addEventListener("storage", handleStorage);
  window.addEventListener(advancedModeChangeEvent, notify);
  return () => {
    window.removeEventListener("storage", handleStorage);
    window.removeEventListener(advancedModeChangeEvent, notify);
  };
}
