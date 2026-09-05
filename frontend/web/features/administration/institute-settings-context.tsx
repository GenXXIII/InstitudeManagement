"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { administrationApi } from "./administration-api";
import { defaultSettings } from "./administration-defaults";
import { settingSections, type InstituteSettings, type SettingSection } from "./administration-types";
import { configureWorkflowCodes } from "@/lib/workflow-code";

export { defaultSettings, settingSections };
export type { InstituteSettings, SettingSection };

type SettingsContextValue = { settings: InstituteSettings; refresh: () => Promise<void>; ready: boolean };
const SettingsContext = createContext<SettingsContextValue>({ settings: defaultSettings, refresh: async () => {}, ready: false });

export function InstituteSettingsProvider({ children }: { children: React.ReactNode }) {
  const [settings, setSettings] = useState<InstituteSettings>(defaultSettings);
  const [ready, setReady] = useState(false);

  const refresh = useCallback(async () => {
    try {
      const results = await administrationApi.list();
      const bySection = new Map(results.map(result => [result.section, result.values]));
      const next = Object.fromEntries(settingSections.map(section => [section, { ...defaultSettings[section], ...(bySection.get(section) ?? {}) }])) as InstituteSettings;
      configureWorkflowCodes(next["code-formats"], next["academic-year"].currentYear);
      setSettings(next);
    } catch {
      // Runtime screens continue with safe sample defaults while the API is unavailable.
    } finally { setReady(true); }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void refresh(), 0);
    return () => window.clearTimeout(timer);
  }, [refresh]);

  const value = useMemo(() => ({ settings, refresh, ready }), [ready, refresh, settings]);
  return <SettingsContext.Provider value={value}>{children}</SettingsContext.Provider>;
}

export function useInstituteSettings() {
  return useContext(SettingsContext);
}

export function configuredGrade(score: number, rules: Record<string, string>) {
  const minimum = (key: string, fallback: number) => Number.isFinite(Number(rules[key])) ? Number(rules[key]) : fallback;
  if (score >= minimum("aPlusMinimum", 95)) return "A+";
  if (score >= minimum("aMinimum", 90)) return "A";
  if (score >= minimum("bPlusMinimum", 85)) return "B+";
  if (score >= minimum("bMinimum", 80)) return "B";
  if (score >= minimum("cPlusMinimum", 75)) return "C+";
  if (score >= minimum("cMinimum", 70)) return "C";
  return score >= minimum("dMinimum", 60) ? "D" : "F";
}
