"use client";

import { useEffect, useMemo, useRef, useState, type MouseEvent } from "react";
import { administrationApi } from "./administration-api";
import {
  administrationSections,
  editableSectionFields,
  sectionDefinition,
  simpleSectionFieldKeys,
} from "./administration-config";
import { defaultSettings } from "./administration-defaults";
import { useAdministrationMode } from "./administration-mode-context";
import type { SettingSection } from "./administration-types";
import { useInstituteSettings } from "./institute-settings-context";
import { validateSettings } from "./settings-validation";

const maintenanceKeys = new Set(["maintenanceEnabled", "maintenanceMessage", "allowAdministratorsDuringMaintenance"]);

export function useSettingsSection(section: SettingSection) {
  const definition = sectionDefinition(section);
  const { refresh } = useInstituteSettings();
  const { advanced } = useAdministrationMode();
  const [values, setValues] = useState<Record<string, string>>();
  const [baseline, setBaseline] = useState<Record<string, string>>({});
  const [configured, setConfigured] = useState(false);
  const [updatedAtUtc, setUpdatedAtUtc] = useState<string | null>(null);
  const [loadError, setLoadError] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);
  const saveController = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      void administrationApi.get(section, controller.signal).then(result => {
        const merged = normalizeSection(section, { ...defaultSettings[section], ...result.values });
        setValues(merged);
        setBaseline(merged);
        setConfigured(result.isConfigured);
        setUpdatedAtUtc(result.updatedAtUtc);
        setLoadError(false);
        setSaveError("");
        setValidationErrors([]);
      }).catch(() => { if (!controller.signal.aborted) setLoadError(true); });
    }, 0);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [reloadKey, section]);

  useEffect(() => () => saveController.current?.abort(), []);

  const editableFields = useMemo(() => editableSectionFields(section), [section]);
  const sectionEditableFields = useMemo(
    () => section === "system" ? editableFields.filter(field => !maintenanceKeys.has(field.key)) : editableFields,
    [editableFields, section],
  );
  const visibleGroups = useMemo(() => {
    const sectionGroups = section === "system"
      ? definition.groups.map(group => ({ ...group, fields: group.fields.filter(field => !maintenanceKeys.has(field.key)) })).filter(group => group.fields.length > 0)
      : definition.groups;
    if (advanced) return sectionGroups;
    const visibleKeys = new Set(simpleSectionFieldKeys(section, values ?? {}));
    return sectionGroups
      .map(group => ({ ...group, fields: group.fields.filter(field => visibleKeys.has(field.key)) }))
      .filter(group => group.fields.length > 0);
  }, [advanced, definition.groups, section, values]);
  const visibleEditableCount = useMemo(
    () => visibleGroups.flatMap(group => group.fields).filter(field => field.type !== "derived" && !field.readOnly).length,
    [visibleGroups],
  );
  const hiddenFieldCount = Math.max(0, sectionEditableFields.length - visibleEditableCount);
  const changedCount = useMemo(
    () => values ? sectionEditableFields.filter(item => values[item.key] !== baseline[item.key]).length : 0,
    [baseline, sectionEditableFields, values],
  );
  const dirty = changedCount > 0;
  const currentIndex = administrationSections.findIndex(item => item.section === section);
  const previous = administrationSections[currentIndex - 1];
  const next = administrationSections[currentIndex + 1];

  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => { if (dirty) event.preventDefault(); };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  async function save() {
    if (!values) return;
    const pendingValues = normalizeSection(section, section === "system" ? { ...values, ...pickValues(baseline, maintenanceKeys) } : values);
    const problems = validateSettings(section, pendingValues);
    setValidationErrors(problems);
    setSaveError("");
    if (problems.length) return;
    setSaving(true);
    const controller = new AbortController();
    saveController.current = controller;
    try {
      const result = await administrationApi.save(section, pendingValues, controller.signal);
      const applied = normalizeSection(section, { ...defaultSettings[section], ...result.values });
      setValues(applied);
      setBaseline(applied);
      setConfigured(result.isConfigured);
      setUpdatedAtUtc(result.updatedAtUtc);
      setSaveError("");
      setValidationErrors([]);
      await refresh();
      setSaved(true);
      window.setTimeout(() => setSaved(false), 2800);
    } catch (reason) {
      if (!controller.signal.aborted) setSaveError(reason instanceof Error ? reason.message : "Could not save this Settings section.");
    } finally {
      if (saveController.current === controller) saveController.current = null;
      setSaving(false);
    }
  }

  function change(key: string, value: string) {
    setValues(current => normalizeSection(section, { ...current!, [key]: value }));
    clearSaveFeedback();
  }

  function restoreRecommended() {
    if (!values) return;
    const knownKeys = new Set(Object.keys(defaultSettings[section]));
    const unknown = Object.fromEntries(Object.entries(baseline).filter(([key]) => !knownKeys.has(key)));
    const recommended = normalizeSection(section, { ...unknown, ...defaultSettings[section] });
    setValues(section === "system" ? { ...recommended, ...pickValues(values, maintenanceKeys) } : recommended);
    clearSaveFeedback();
  }

  function clearSaveFeedback() {
    setValidationErrors([]);
    setSaveError("");
    setSaved(false);
  }

  function protectNavigation(event: MouseEvent<HTMLAnchorElement>) {
    if (dirty && !window.confirm("Discard the unsaved Settings changes?")) event.preventDefault();
  }

  return {
    advanced,
    baseline,
    changedCount,
    configured,
    definition,
    hiddenFieldCount,
    loadError,
    next,
    previous,
    protectNavigation,
    restoreRecommended,
    retry: () => { setLoadError(false); setReloadKey(current => current + 1); },
    save,
    saveError,
    saved,
    saving,
    sectionEditableFields,
    setValues,
    updatedAtUtc,
    validationErrors,
    values,
    visibleEditableCount,
    visibleGroups,
    change,
  };
}

function normalizeSection(section: SettingSection, values: Record<string, string>) {
  if (section !== "semester") return values;
  const prefix = values.currentTerm === "Semester 2" ? "semester2" : values.currentTerm === "Summer Term" ? "summer" : "semester1";
  return { ...values, startsOn: values[`${prefix}StartsOn`] ?? "", endsOn: values[`${prefix}EndsOn`] ?? "" };
}

function pickValues(values: Record<string, string>, keys: Set<string>) {
  return Object.fromEntries([...keys].map(key => [key, values[key] ?? ""]));
}
