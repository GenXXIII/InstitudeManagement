"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { administrationApi } from "./administration-api";

export const settingSections = ["institute", "academic-year", "semester", "departments", "courses", "classrooms", "attendance-rules", "grade-rules", "notifications", "system"] as const;
export type SettingSection = typeof settingSections[number];
export type InstituteSettings = Record<SettingSection, Record<string, string>>;

export const defaultSettings: InstituteSettings = {
  institute: { name: "Institude of New Khmer", shortName: "INK", email: "info@ink.edu.kh", phone: "+855 23 000 000", address: "Phnom Penh, Cambodia" },
  "academic-year": { currentYear: "2026–2027", startsOn: "2026-08-03", endsOn: "2027-06-18" },
  semester: { currentTerm: "Semester 1", startsOn: "2026-08-03", endsOn: "2026-12-18", semester1StartsOn: "2026-08-03", semester1EndsOn: "2026-12-18", semester2StartsOn: "2027-01-04", semester2EndsOn: "2027-06-18" },
  departments: { requireDepartmentHead: "true", allowCrossDepartmentTeaching: "false", defaultStatus: "Active" },
  courses: { defaultCapacity: "40", requireAssignedTeacher: "true" },
  classrooms: { defaultCapacity: "40", attendanceDeviceRequired: "true", allowSharedRooms: "false" },
  "attendance-rules": { method: "ID Card", lateThresholdMinutes: "15", autoAbsent: "true", autoPercentage: "true", notifyTeacher: "true", notifyAdministrator: "true", allowCorrection: "true", requireCorrectionReason: "false" },
  "grade-rules": { aMinimum: "90", bMinimum: "80", cMinimum: "70", dMinimum: "60", eMinimum: "50" },
  notifications: { attendanceAlerts: "true", deviceAlerts: "true", gradeReminders: "true", dailySummary: "true" },
  system: { timeZone: "Asia/Bangkok", language: "English", dateFormat: "DD MMM YYYY", autoRefreshSeconds: "30" },
};

export const settingsTemplates: InstituteSettings = {
  ...defaultSettings,
  institute: { name: "Institude of New Khmer", shortName: "INK", email: "", phone: "", address: "" },
  "academic-year": { currentYear: "", startsOn: "", endsOn: "" },
  semester: { currentTerm: "Semester 1", startsOn: "", endsOn: "", semester1StartsOn: "", semester1EndsOn: "", semester2StartsOn: "", semester2EndsOn: "" },
};

type SettingsContextValue = { settings: InstituteSettings; refresh: () => Promise<void>; ready: boolean };
const SettingsContext = createContext<SettingsContextValue>({ settings: defaultSettings, refresh: async () => {}, ready: false });

export function InstituteSettingsProvider({ children }: { children: React.ReactNode }) {
  const [settings, setSettings] = useState<InstituteSettings>(defaultSettings);
  const [ready, setReady] = useState(false);
  const refresh = useCallback(async () => {
    const results = await Promise.all(settingSections.map(section => administrationApi.get(section).catch(() => ({ section, values: {} }))));
    setSettings(current => Object.fromEntries(results.map(result => [result.section, { ...current[result.section as SettingSection], ...result.values }])) as InstituteSettings);
    setReady(true);
  }, []);
  useEffect(() => { const timer = window.setTimeout(() => void refresh(), 0); return () => window.clearTimeout(timer); }, [refresh]);
  const value = useMemo(() => ({ settings, refresh, ready }), [ready, refresh, settings]);
  return <SettingsContext.Provider value={value}>{children}</SettingsContext.Provider>;
}

export function useInstituteSettings() { return useContext(SettingsContext); }

export function configuredGrade(score: number, rules: Record<string, string>) {
  const minimum = (key: string, fallback: number) => Number.isFinite(Number(rules[key])) ? Number(rules[key]) : fallback;
  return score >= minimum("aMinimum", 90) ? "A" : score >= minimum("bMinimum", 80) ? "B" : score >= minimum("cMinimum", 70) ? "C" : score >= minimum("dMinimum", 60) ? "D" : score >= minimum("eMinimum", 50) ? "E" : "F";
}
